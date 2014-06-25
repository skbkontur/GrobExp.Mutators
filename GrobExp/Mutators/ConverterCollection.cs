using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Compiler;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.CustomFields;

namespace GrobExp.Mutators
{
    public abstract class ConverterCollection<TSource, TDest> : IConverterCollection<TSource, TDest> where TDest : new()
    {
        protected ConverterCollection(IPathFormatterCollection pathFormatterCollection)
        {
            this.pathFormatterCollection = pathFormatterCollection;
        }

        public Func<TSource, TDest> GetConverter(MutatorsContext context)
        {
            return GetOrCreateHashtableSlot(context).Converter;
        }

        public Action<TSource, TDest> GetMerger(MutatorsContext context)
        {
            return GetOrCreateHashtableSlot(context).Merger;
        }

        public MutatorsTree<TSource> Migrate(MutatorsTree<TDest> mutatorsTree, MutatorsContext context)
        {
            return mutatorsTree == null ? null : mutatorsTree.Migrate<TSource>(GetOrCreateHashtableSlot(context).ConverterTree);
        }

        public MutatorsTree<TSource> GetValidationsTree(MutatorsContext context, int priority)
        {
            return new SimpleMutatorsTree<TSource>(GetOrCreateHashtableSlot(context).ValidationsTree, pathFormatterCollection.GetPathFormatter<TSource>(), pathFormatterCollection, priority);
        }

        public MutatorsTree<TDest> MigratePaths(MutatorsTree<TDest> mutatorsTree, MutatorsContext context)
        {
            return mutatorsTree == null ? null : mutatorsTree.MigratePaths<TSource>(GetOrCreateHashtableSlot(context).ConverterTree);
        }

        protected abstract void Configure(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator);

        protected virtual void BeforeConvert(TSource source)
        {
        }

        protected virtual void AfterConvert(TDest dest, TSource source)
        {
        }

        private HashtableSlot GetOrCreateHashtableSlot(MutatorsContext context)
        {
            var key = context.GetKey();
            var slot = (HashtableSlot)hashtable[key];
            if(slot == null)
            {
                lock(lockObject)
                {
                    slot = (HashtableSlot)hashtable[key];
                    if(slot == null)
                    {
                        var tree = ModelConfigurationNode.CreateRoot(typeof(TDest));
                        ConfigureInternal(context, new ConverterConfigurator<TSource, TDest>(tree));
                        var validationsTree = ModelConfigurationNode.CreateRoot(typeof(TSource));
                        tree.ExtractValidationsFromConverters(validationsTree);
                        var treeMutator = (Expression<Action<TDest, TSource>>)tree.BuildTreeMutator(typeof(TSource));
                        var compiledTreeMutator = LambdaCompiler.Compile(treeMutator, CompilerOptions.All);
                        hashtable[key] = slot = new HashtableSlot
                            {
                                ConverterTree = tree,
                                ValidationsTree = validationsTree,
                                Converter = (source =>
                                    {
                                        var dest = new TDest();
                                        BeforeConvert(source);
                                        compiledTreeMutator(dest, source);
                                        AfterConvert(dest, source);
                                        return dest;
                                    }),
                                Merger = ((source, dest) =>
                                    {
                                        BeforeConvert(source);
                                        compiledTreeMutator(dest, source);
                                        AfterConvert(dest, source);
                                    })
                            };
                    }
                }
            }
            return slot;
        }

        private static TypeCode GetTypeCode(Type type)
        {
            return type.IsNullable() ? GetTypeCode(type.GetGenericArguments()[0]) : Type.GetTypeCode(type);
        }

        private void ConfigureCustomFields(ConverterConfigurator<TSource, TDest> configurator, LambdaExpression pathToSourceChild, LambdaExpression pathToDestChild)
        {
            var sourceChildType = pathToSourceChild.Body.Type;
            var destChildType = pathToDestChild.Body.Type;
            var sourceProperties = sourceChildType.GetOrderedProperties();
            var destProperties = destChildType.GetOrderedProperties();
            var sourceCustomFieldsContainer = sourceProperties.SingleOrDefault(prop => prop.GetCustomAttributes(typeof(CustomFieldsContainerAttribute), false).Any());
            var sourceCustomFields = sourceProperties.Where(prop => prop.GetCustomAttributes(typeof(CustomFieldAttribute), false).Any()).ToArray();
            var destCustomFieldsContainer = destProperties.SingleOrDefault(prop => prop.GetCustomAttributes(typeof(CustomFieldsContainerAttribute), false).Any());
            var destCustomFields = destProperties.Where(prop => prop.GetCustomAttributes(typeof(CustomFieldAttribute), false).Any()).ToArray();
            if(sourceCustomFields.Length > 0)
            {
                if(destCustomFields.Length > 0 || sourceCustomFieldsContainer != null)
                    throw new InvalidOperationException();
                if(destCustomFieldsContainer == null)
                    return;
                var destParameter = Expression.Parameter(destChildType);
                var sourceParameter = Expression.Parameter(sourceChildType);
                var pathToDestContainer = Expression.Property(destParameter, destCustomFieldsContainer);
                var indexerGetter = destCustomFieldsContainer.PropertyType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                foreach(var customField in sourceCustomFields)
                {
                    var value = Expression.Property(sourceParameter, customField);
                    var pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Call(pathToDestContainer, indexerGetter, Expression.Constant(customField.Name)), destParameter)).Body;
                    configurator.SetMutator(Expression.Property(pathToTarget, "TypeCode"), EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.Constant(GetTypeCode(customField.PropertyType)))));
                    configurator.SetMutator(Expression.Property(pathToTarget, "Value"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(value, sourceParameter))));
                    var titleAttribute = customField.GetCustomAttributes(typeof(CustomFieldAttribute), false).SingleOrDefault() as CustomFieldAttribute;
                    if(titleAttribute != null)
                        configurator.SetMutator(Expression.Property(pathToTarget, "Title"), EqualsToConfiguration.Create<TDest>(Expression.Lambda(Expression.New(titleAttribute.TitleType))));
                }
            }
            else if(destCustomFields.Length > 0)
            {
                if(destCustomFieldsContainer != null)
                    throw new InvalidOperationException();
                if(sourceCustomFieldsContainer == null)
                    return;
                var destParameter = Expression.Parameter(destChildType);
                var sourceParameter = Expression.Parameter(sourceChildType);
                var pathToSourceContainer = Expression.Property(sourceParameter, sourceCustomFieldsContainer);
                var indexerGetter = sourceCustomFieldsContainer.PropertyType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                foreach(var customField in destCustomFields)
                {
                    var pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Property(destParameter, customField), destParameter)).Body;
                    var value = Expression.Convert(Expression.Property(Expression.Call(pathToSourceContainer, indexerGetter, Expression.Constant(customField.Name)), "Value"), pathToTarget.Type);
                    configurator.SetMutator(pathToTarget, EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(value, sourceParameter))));
                }
            }
            else
            {
                if(sourceCustomFieldsContainer == null || destCustomFieldsContainer == null)
                    return;
                var destParameter = Expression.Parameter(destChildType);
                var sourceParameter = Expression.Parameter(sourceChildType);
                Expression pathToDestCustomContainer = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(destCustomFieldsContainer.PropertyType.GetItemType()), Expression.Property(destParameter, destCustomFieldsContainer));
                Expression pathToSourceCustomContainer = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(sourceCustomFieldsContainer.PropertyType.GetItemType()), Expression.Property(sourceParameter, sourceCustomFieldsContainer));
                Expression pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Property(pathToDestCustomContainer, "Key"), destParameter)).Body;
                Expression value = Expression.Property(pathToSourceCustomContainer, "Key");
                configurator.SetMutator(pathToTarget, EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(value, sourceParameter))));
                value = Expression.Property(pathToSourceCustomContainer, "Value");
                pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Property(pathToDestCustomContainer, "Value"), destParameter)).Body;
                configurator.SetMutator(Expression.Property(pathToTarget, "TypeCode"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "TypeCode"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "Value"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "Value"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "Title"), EqualsToConfiguration.Create<TDest>(pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "Title"), sourceParameter))));
            }
        }

        private void ConfigureCustomFields(ConverterConfigurator<TSource, TDest> configurator)
        {
            var tree = configurator.GetTree();
            var sourceParameter = Expression.Parameter(typeof(TSource));
            var destParameter = Expression.Parameter(typeof(TDest));
            ConfigureCustomFields(configurator, Expression.Lambda(sourceParameter, sourceParameter), Expression.Lambda(destParameter, destParameter));
            foreach(var property in typeof(TDest).GetProperties().Where(property => property.PropertyType.IsArray))
            {
                var pathToDestArray = Expression.Property(destParameter, property);
                var node = tree.Traverse(pathToDestArray, false);
                if(node == null)
                    continue;
                var arrays = node.GetArrays(true);
                Expression pathToSourceArray;
                if(!arrays.TryGetValue(typeof(TSource), out pathToSourceArray))
                    continue;
                var pathToDestArrayItem = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(pathToDestArray.Type.GetItemType()), pathToDestArray);
                var pathToSourceArrayItem = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(pathToSourceArray.Type.GetItemType()), pathToSourceArray);
                ConfigureCustomFields(configurator, Expression.Lambda(pathToSourceArrayItem, pathToSourceArray.ExtractParameters()), Expression.Lambda(pathToDestArrayItem, pathToDestArray.ExtractParameters()));
            }
        }

        private void ConfigureInternal(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator)
        {
            Configure(context, configurator);
            ConfigureCustomFields(configurator);
        }

        private readonly IPathFormatterCollection pathFormatterCollection;

        private readonly object lockObject = new object();

        private readonly Hashtable hashtable = new Hashtable();

        private class HashtableSlot
        {
            public ModelConfigurationNode ConverterTree { get; set; }
            public ModelConfigurationNode ValidationsTree { get; set; }
            public Func<TSource, TDest> Converter { get; set; }
            public Action<TSource, TDest> Merger { get; set; }
        }
    }
}