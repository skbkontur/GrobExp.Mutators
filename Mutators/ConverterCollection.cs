using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

using GrEmit.Utils;

using GrobExp.Compiler;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.CustomFields;
using GrobExp.Mutators.ModelConfiguration;
using GrobExp.Mutators.Visitors;

using Vostok.Logging.Abstractions;

namespace GrobExp.Mutators
{
    public abstract class ConverterCollection<TSource, TDest> : IConverterCollection<TSource, TDest> where TDest : new()
    {
        protected ConverterCollection(IPathFormatterCollection pathFormatterCollection, IStringConverter stringConverter, ILog logger)
        {
            this.pathFormatterCollection = pathFormatterCollection;
            this.stringConverter = stringConverter;
            this.logger = logger.ForContext("Mutators");
        }

        protected ConverterCollection(IPathFormatterCollection pathFormatterCollection, IStringConverter stringConverter)
            : this(pathFormatterCollection, stringConverter, new SilentLog())
        {
        }

        public Func<TSource, TDest> GetConverter(MutatorsContext context)
        {
            return GetOrCreateHashtableSlot(context).Converter;
        }

        public Action<TSource, TDest> GetMerger(MutatorsContext context)
        {
            return GetOrCreateHashtableSlot(context).Merger;
        }

        public MutatorsTreeBase<TSource> Migrate(MutatorsTreeBase<TDest> mutatorsTree, MutatorsContext context)
        {
            return mutatorsTree?.Migrate<TSource>(GetOrCreateHashtableSlot(context).ConverterTree);
        }

        public MutatorsTreeBase<TSource> GetValidationsTree(MutatorsContext context, int priority)
        {
            var slot = GetOrCreateHashtableSlot(context);
            var result = (MutatorsTreeBase<TSource>)slot.ValidationMutatorsTrees[priority];
            if (result == null)
            {
                lock (lockObject)
                {
                    result = (MutatorsTreeBase<TSource>)slot.ValidationMutatorsTrees[priority];
                    if (result == null)
                    {
                        slot.ValidationMutatorsTrees[priority] = result = new MutatorsTree<TSource>(slot.ValidationsTree, pathFormatterCollection.GetPathFormatter<TSource>(), pathFormatterCollection, priority);
                    }
                }
            }

            return result;
        }

        public MutatorsTreeBase<TDest> MigratePaths(MutatorsTreeBase<TDest> mutatorsTree, MutatorsContext context)
        {
            return mutatorsTree?.MigratePaths<TSource>(GetOrCreateHashtableSlot(context).ConverterTree);
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
            if (slot == null)
            {
                lock (lockObject)
                {
                    slot = (HashtableSlot)hashtable[key];
                    if (slot == null)
                    {
                        var tree = ModelConfigurationNode.CreateRoot(GetType(), typeof(TDest));
                        ConfigureInternal(context, new ConverterConfigurator<TSource, TDest>(tree));
                        var validationsTree = ModelConfigurationNode.CreateRoot(GetType(), typeof(TSource));
                        tree.ExtractValidationsFromConverters(validationsTree);

                        var lazyCompiledConverter = new Lazy<Action<TDest, TSource>>(() => CompileTree(tree, context), LazyThreadSafetyMode.ExecutionAndPublication);

                        slot = new HashtableSlot
                            {
                                ConverterTree = tree,
                                ValidationsTree = validationsTree,
                                Converter = source =>
                                    {
                                        var dest = new TDest();
                                        BeforeConvert(source);
                                        lazyCompiledConverter.Value(dest, source);
                                        AfterConvert(dest, source);
                                        return dest;
                                    },
                                Merger = (source, dest) =>
                                    {
                                        BeforeConvert(source);
                                        lazyCompiledConverter.Value(dest, source);
                                        AfterConvert(dest, source);
                                    },
                                ValidationMutatorsTrees = new Hashtable()
                            };
                        //if(!MutatorsAssignRecorder.IsRecording())
                        hashtable[key] = slot;
                    }
                }
            }

            return slot;
        }

        private Action<TDest, TSource> CompileTree(ModelConfigurationNode tree, MutatorsContext context)
        {
            var treeConverter = (Expression<Action<TDest, TSource>>)tree.BuildTreeMutator(typeof(TSource));

            Action<TDest, TSource> compiledTreeConverter;
            var sw = Stopwatch.StartNew();
            try
            {
                compiledTreeConverter = LambdaCompiler.Compile(treeConverter, CompilerOptions.All);
            }
            finally
            {
                sw.Stop();
                LogConverterCompilation(context, sw);
            }
            return compiledTreeConverter;
        }

        private void LogConverterCompilation(MutatorsContext context, Stopwatch sw)
        {
            var converterCollectionTypeName = GetType().Name;
            var message = new StringBuilder($"{converterCollectionTypeName} was compiled in {sw.ElapsedMilliseconds} ms\n");

            message.AppendLine("Context:\n");
            var mutatorsContextTypeName = context.GetType().Name;
            foreach (var propertyInfo in context.GetType().GetProperties())
                message.AppendLine($"\t{mutatorsContextTypeName}.{propertyInfo.Name}: {propertyInfo.GetValue(context)}");
            logger.Info(message.ToString());
        }

        private static TypeCode GetTypeCode(Type type)
        {
            if (type.IsArray) return GetTypeCode(type.GetElementType());
            return type.IsNullable() ? GetTypeCode(type.GetGenericArguments()[0]) : Type.GetTypeCode(type);
        }

        private static bool IsALeaf(Type type)
        {
            return type.IsArray && IsALeaf(type.GetElementType()) || type.IsPrimitive || type == typeof(string) || type.IsValueType;
        }

        private static bool IsLazy(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>);
        }

        private static void FindCustomFieldsContainer(Type type, Expression current, List<KeyValuePair<PropertyInfo, Expression>> result)
        {
            if (type == null || IsALeaf(type))
                return;
            var properties = type.GetOrderedProperties();
            foreach (var property in properties)
            {
                if (property.GetGetMethod().GetParameters().Length != 0)
                    continue;

                var next = Expression.Property(current, property);
                if (property.GetCustomAttributes(typeof(CustomFieldsContainerAttribute), false).Any())
                {
                    if (!IsLazy(property.PropertyType))
                        result.Add(new KeyValuePair<PropertyInfo, Expression>(property, next));
                    else
                    {
                        var valueProperty = property.PropertyType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                        result.Add(new KeyValuePair<PropertyInfo, Expression>(valueProperty, Expression.Property(next, valueProperty)));
                    }
                }
                else
                    FindCustomFieldsContainer(property.PropertyType, next, result);
            }
        }

        private static PropertyInfo FindCustomFieldsContainer(Type type, out LambdaExpression path)
        {
            var parameter = Expression.Parameter(type);
            var customFieldsContainers = new List<KeyValuePair<PropertyInfo, Expression>>();
            FindCustomFieldsContainer(type, parameter, customFieldsContainers);
            switch (customFieldsContainers.Count)
            {
            case 0:
                path = null;
                return null;
            case 1:
                path = Expression.Lambda(customFieldsContainers[0].Value, parameter);
                return customFieldsContainers[0].Key;
            default:
                throw new InvalidOperationException("Found more than one custom fields container in type '" + type + "'");
            }
        }

        private static void FindCustomFields(bool root, Type type, Expression pathFromLocalRoot, Type titleType, PropertyInfo rootProperty, Func<Expression, bool> checker, Expression pathFromRoot, List<CustomFieldInfo> result)
        {
            if (type.IsArray)
            {
                type = type.GetElementType();
                pathFromLocalRoot = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(type), pathFromLocalRoot);
                pathFromRoot = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(type), pathFromRoot);
            }

            var properties = type.GetOrderedProperties().Where(prop => prop.GetCustomAttributes(typeof(CustomFieldAttribute), false).Any()).ToArray();
            foreach (var property in properties)
            {
                var nextPathFromLocalRoot = Expression.Property(pathFromLocalRoot, property);
                var nextPathFromRoot = Expression.Property(pathFromRoot, property);
                var customFieldAttribute = property.GetCustomAttributes(typeof(CustomFieldAttribute), false).SingleOrDefault() as CustomFieldAttribute;
                var currentTitleType = titleType;
                if (customFieldAttribute != null && customFieldAttribute.TitleType != null)
                    currentTitleType = customFieldAttribute.TitleType;
                if (root)
                    rootProperty = property;
                if (!IsALeaf(property.PropertyType))
                    FindCustomFields(false, property.PropertyType, nextPathFromLocalRoot, currentTitleType, rootProperty, checker, nextPathFromRoot, result);
                else
                {
                    if (checker(nextPathFromRoot))
                        result.Add(new CustomFieldInfo(nextPathFromLocalRoot.CustomFieldName(), rootProperty, currentTitleType, nextPathFromLocalRoot));
                }
            }
        }

        private static CustomFieldInfoZ[] FindCustomFields(Type type, Func<Expression, bool> checker, LambdaExpression pathToNode)
        {
            var parameter = Expression.Parameter(type);
            var customFields = new List<CustomFieldInfo>();
            FindCustomFields(true, type, parameter, null, null, checker, pathToNode.Body, customFields);
            return customFields.Select(info => new CustomFieldInfoZ(info.Path, info.RootProperty, info.TitleType, Expression.Lambda(info.Value, parameter))).ToArray();
        }

        private void ConfigureCustomFields(ConverterConfigurator<TSource, TDest> configurator, LambdaExpression pathToSourceChild, LambdaExpression pathToDestChild, Func<Expression, bool> sourceCustomFieldFits, Func<Expression, bool> destCustomFieldFits)
        {
            var sourceChildType = pathToSourceChild.Body.Type;
            var destChildType = pathToDestChild.Body.Type;
            LambdaExpression pathToSourceCustomFieldsContainer;
            var sourceCustomFieldsContainer = FindCustomFieldsContainer(sourceChildType, out pathToSourceCustomFieldsContainer);
            var sourceCustomFields = FindCustomFields(sourceChildType, sourceCustomFieldFits, pathToSourceChild);
            LambdaExpression pathToDestCustomFieldsContainer;
            var destCustomFieldsContainer = FindCustomFieldsContainer(destChildType, out pathToDestCustomFieldsContainer);
            var destCustomFields = FindCustomFields(destChildType, destCustomFieldFits, pathToDestChild);
            var converterType = configurator.Root.ConfiguratorType;
            if (sourceCustomFields.Length > 0)
            {
                if (destCustomFields.Length > 0 || sourceCustomFieldsContainer != null)
                    throw new InvalidOperationException();
                if (destCustomFieldsContainer == null)
                    return;
                var destParameter = pathToDestCustomFieldsContainer.Parameters.Single();
                var indexerGetter = destCustomFieldsContainer.PropertyType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                foreach (var customField in sourceCustomFields)
                {
                    var path = customField.Path;
                    var rootProperty = customField.RootProperty;
                    var titleType = customField.TitleType;
                    var value = customField.Value;
                    TypeCode typeCode;
                    LambdaExpression convertedValue;
                    if (stringConverter != null && stringConverter.CanConvert(value.Body.Type))
                    {
                        typeCode = TypeCode.String;

                        convertedValue = Expression.Lambda(
                            Expression.Call(Expression.Constant(stringConverter, typeof(IStringConverter)),
                                            convertToStringMethod.MakeGenericMethod(value.Body.Type),
                                            Expression.Convert(value.Body, typeof(object))),
                            value.Parameters);
                    }
                    else
                    {
                        typeCode = GetTypeCode(value.Body.Type);
                        convertedValue = value;
                    }

                    if (rootProperty.PropertyType.IsArray && !IsALeaf(rootProperty.PropertyType))
                    {
                        // An array of complex types
                        var delimiterIndex = path.IndexOf('ё');
                        var pathToArray = path.Substring(0, delimiterIndex);
                        var pathToLeaf = path.Substring(delimiterIndex + 1, path.Length - delimiterIndex - 1);
                        var pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Call(pathToDestCustomFieldsContainer.Body, indexerGetter, Expression.Constant(pathToArray)), destParameter)).Body;
                        configurator.SetMutator(Expression.Property(pathToTarget, "TypeCode"), EqualsToConfiguration.Create<TDest>(converterType, Expression.Lambda(Expression.Constant(TypeCode.Object))));
                        configurator.SetMutator(Expression.Property(pathToTarget, "IsArray"), EqualsToConfiguration.Create<TDest>(converterType, Expression.Lambda(Expression.Constant(true))));

                        var pathToDestArrayItem = Expression.Convert(Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(typeof(object)), Expression.Convert(Expression.Property(pathToTarget, "Value"), typeof(object[]))), typeof(Hashtable));
                        var itemIndexerGetter = typeof(Hashtable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                        var pathToDestArrayItemValue = Expression.Call(pathToDestArrayItem, itemIndexerGetter, Expression.Constant(pathToLeaf));

                        configurator.SetMutator(pathToDestArrayItemValue, EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(convertedValue)));

                        var pathToTypeCodes = Expression.Property(pathToTarget, "TypeCodes");
                        var pathToItemTypeCode = Expression.Call(pathToTypeCodes, pathToTypeCodes.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), Expression.Constant(pathToLeaf));
                        configurator.SetMutator(pathToItemTypeCode, EqualsToConfiguration.Create<TDest>(converterType, Expression.Lambda(Expression.Constant(typeCode))));

                        if (value.Body is MemberExpression)
                        {
                            var member = ((MemberExpression)value.Body).Member;
                            var customFieldAttribute = member.GetCustomAttributes(typeof(CustomFieldAttribute), false).SingleOrDefault() as CustomFieldAttribute;
                            if (customFieldAttribute != null && customFieldAttribute.TitleType != null)
                            {
                                var pathToTitles = Expression.Property(pathToTarget, "Titles");
                                var pathToItemTitle = Expression.Call(pathToTitles, pathToTitles.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), Expression.Constant(pathToLeaf));
                                configurator.SetMutator(pathToItemTitle, EqualsToConfiguration.Create<TDest>(converterType, Expression.Lambda(Expression.Constant(StaticMultiLanguageTextCache.Get(customFieldAttribute.TitleType)))));
                            }
                        }
                    }
                    else
                    {
                        var pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Call(pathToDestCustomFieldsContainer.Body, indexerGetter, Expression.Constant(path)), destParameter)).Body;
                        configurator.SetMutator(Expression.Property(pathToTarget, "TypeCode"), EqualsToConfiguration.Create<TDest>(converterType, Expression.Lambda(Expression.Constant(typeCode))));
                        configurator.SetMutator(Expression.Property(pathToTarget, "IsArray"), EqualsToConfiguration.Create<TDest>(converterType, Expression.Lambda(Expression.Constant(convertedValue.Body.Type.IsArray))));
                        configurator.SetMutator(Expression.Property(pathToTarget, "Value"), EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(convertedValue)));
                        if (titleType != null)
                            configurator.SetMutator(Expression.Property(pathToTarget, "Title"), EqualsToConfiguration.Create<TDest>(converterType, Expression.Lambda(Expression.Constant(StaticMultiLanguageTextCache.Get(titleType)))));
                    }
                }
            }
            else if (destCustomFields.Length > 0)
            {
                if (destCustomFieldsContainer != null)
                    throw new InvalidOperationException();
                if (sourceCustomFieldsContainer == null)
                    return;
                var sourceParameter = pathToSourceCustomFieldsContainer.Parameters.Single();
                var indexerGetter = sourceCustomFieldsContainer.PropertyType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                foreach (var customField in destCustomFields)
                {
                    var path = customField.Path;
                    var rootProperty = customField.RootProperty;
                    var pathToTarget = pathToDestChild.Merge(customField.Value).Body;
                    if (rootProperty.PropertyType.IsArray && !IsALeaf(rootProperty.PropertyType.GetElementType()))
                    {
                        var delimiterIndex = path.IndexOf('ё');
                        var pathToArray = path.Substring(0, delimiterIndex);
                        var pathToLeaf = path.Substring(delimiterIndex + 1, path.Length - delimiterIndex - 1);
                        Expression value = Expression.Property(Expression.Call(pathToSourceCustomFieldsContainer.Body, indexerGetter, Expression.Constant(pathToArray)), "Value");
                        value = Expression.Convert(Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(typeof(object)), Expression.Convert(value, typeof(object[]))), typeof(Hashtable));
                        value = Expression.Call(value, typeof(Hashtable).GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), Expression.Constant(pathToLeaf));
                        var convertedValue = MakeConvert(value, pathToTarget.Type);
                        configurator.SetMutator(pathToTarget, EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(convertedValue, sourceParameter))));
                    }
                    else
                    {
                        Expression value = Expression.Property(Expression.Call(pathToSourceCustomFieldsContainer.Body, indexerGetter, Expression.Constant(path)), "Value");
                        var convertedValue = MakeConvert(value, pathToTarget.Type);
                        configurator.SetMutator(pathToTarget, EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(convertedValue, sourceParameter))));
                    }
                }
            }
            else
            {
                if (sourceCustomFieldsContainer == null || destCustomFieldsContainer == null)
                    return;
                var destParameter = pathToDestCustomFieldsContainer.Parameters.Single();
                var sourceParameter = pathToSourceCustomFieldsContainer.Parameters.Single();
                Expression pathToDestCustomContainer = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(destCustomFieldsContainer.PropertyType.GetItemType()), pathToDestCustomFieldsContainer.Body);
                Expression pathToSourceCustomContainer = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(sourceCustomFieldsContainer.PropertyType.GetItemType()), pathToSourceCustomFieldsContainer.Body);
                var pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Property(pathToDestCustomContainer, "Key"), destParameter)).Body;
                Expression value = Expression.Property(pathToSourceCustomContainer, "Key");
                configurator.SetMutator(pathToTarget, EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(value, sourceParameter))));
                value = Expression.Property(pathToSourceCustomContainer, "Value");
                pathToTarget = pathToDestChild.Merge(Expression.Lambda(Expression.Property(pathToDestCustomContainer, "Value"), destParameter)).Body;
                configurator.SetMutator(Expression.Property(pathToTarget, "TypeCode"), EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "TypeCode"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "TypeCodes"), EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "TypeCodes"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "Titles"), EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "Titles"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "IsArray"), EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "IsArray"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "Value"), EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "Value"), sourceParameter))));
                configurator.SetMutator(Expression.Property(pathToTarget, "Title"), EqualsToConfiguration.Create<TDest>(converterType, pathToSourceChild.Merge(Expression.Lambda(Expression.Property(value, "Title"), sourceParameter))));
            }
        }

        private Expression MakeConvert(Expression value, Type type)
        {
            var needCoalesce = type.IsValueType && !type.IsNullable();
            if (stringConverter != null && stringConverter.CanConvert(type))
            {
                value = Expression.Call(Expression.Constant(stringConverter, typeof(IStringConverter)),
                                        convertFromStringMethod.MakeGenericMethod(type),
                                        Expression.Convert(value, typeof(string)));
            }

            if (needCoalesce)
                value = Expression.Coalesce(value, Expression.Convert(Expression.Default(type), typeof(object)));
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType.IsValueType)
                    value = Expression.Call(toArrayMethod.MakeGenericMethod(elementType), Expression.Call(castMethod.MakeGenericMethod(elementType), Expression.Convert(value, typeof(IEnumerable))));
            }

            return Expression.Convert(value, type);
        }

        private void ConfigureCustomFields(ConverterConfigurator<TSource, TDest> configurator)
        {
            var sourceParameter = Expression.Parameter(typeof(TSource));
            var destParameter = Expression.Parameter(typeof(TDest));
            var tree = configurator.Root;
            var subNodes = new List<ModelConfigurationNode>();
            tree.FindSubNodes(subNodes);
            var dependencies = from node in subNodes
                               from mutator in node.GetMutators()
                               where mutator is EqualsToConfiguration
                               from dependency in ((EqualsToConfiguration)mutator).Value.ExtractPrimaryDependencies()
                               select new ExpressionWrapper(dependency.Body, false);
            var hashset = new HashSet<ExpressionWrapper>(dependencies);
            Func<Expression, bool> sourceCustomFieldFits = path => !hashset.Contains(new ExpressionWrapper(path, false));
            Func<Expression, bool> destCustomFieldFits = path =>
                {
                    var node = tree.Traverse(path, false);
                    if (node == null)
                        return true;
                    return node.GetMutators().Length == 0;
                };
            ConfigureCustomFields(configurator, Expression.Lambda(sourceParameter, sourceParameter), Expression.Lambda(destParameter, destParameter), sourceCustomFieldFits, destCustomFieldFits);
            ConfigureCustomFieldsForArrays(configurator, typeof(TDest), Expression.Lambda(destParameter, destParameter), sourceCustomFieldFits, destCustomFieldFits);
        }

        private void ConfigureCustomFieldsForArrays(ConverterConfigurator<TSource, TDest> configurator, Type type, LambdaExpression pathToDestChild, Func<Expression, bool> sourceCustomFieldFits, Func<Expression, bool> destCustomFieldFits)
        {
            if (type == null || IsALeaf(type))
                return;
            var tree = configurator.Root;
            var properties = type.GetOrderedProperties();
            var parameter = Expression.Parameter(type);
            foreach (var property in properties)
            {
                if (property.GetGetMethod().GetParameters().Length != 0)
                    continue;
                
                var pathToNextDestChild = pathToDestChild.Merge(Expression.Lambda(Expression.Property(parameter, property), parameter));
                if (!property.PropertyType.IsArray)
                    ConfigureCustomFieldsForArrays(configurator, property.PropertyType, pathToNextDestChild, sourceCustomFieldFits, destCustomFieldFits);
                else
                {
                    var pathToDestArray = pathToNextDestChild.Body;
                    var node = tree.Traverse(pathToDestArray, false);
                    if (node == null)
                        continue;
                    var arrays = node.GetArrays();
                    if (!arrays.TryGetValue(typeof(TSource), out var pathToSourceArray))
                        continue;
                    var pathToDestArrayItem = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(pathToDestArray.Type.GetItemType()), pathToDestArray);
                    var pathToSourceArrayItem = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(pathToSourceArray.Type.GetItemType()), pathToSourceArray);
                    ConfigureCustomFields(configurator, Expression.Lambda(pathToSourceArrayItem, pathToSourceArray.ExtractParameters()), Expression.Lambda(pathToDestArrayItem, pathToDestArray.ExtractParameters()), sourceCustomFieldFits, destCustomFieldFits);
                }
            }
        }

        protected void ConfigureInternal(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator)
        {
            Configure(context, configurator);
            ConfigureCustomFields(configurator);
        }

        private readonly IPathFormatterCollection pathFormatterCollection;
        private readonly IStringConverter stringConverter;
        private readonly ILog logger;

        private readonly object lockObject = new object();

        private readonly Hashtable hashtable = new Hashtable();
        private readonly MethodInfo convertToStringMethod = HackHelpers.GetMethodDefinition<IStringConverter>(x => x.ConvertToString<int>(null)).GetGenericMethodDefinition();
        private readonly MethodInfo convertFromStringMethod = HackHelpers.GetMethodDefinition<IStringConverter>(x => x.ConvertFromString<int>("")).GetGenericMethodDefinition();
        private readonly MethodInfo castMethod = HackHelpers.GetMethodDefinition<int[]>(x => x.Cast<object>()).GetGenericMethodDefinition();
        private readonly MethodInfo toArrayMethod = HackHelpers.GetMethodDefinition<int[]>(x => x.ToArray()).GetGenericMethodDefinition();

        public class CustomFieldInfoZ
        {
            public CustomFieldInfoZ(string path, PropertyInfo rootProperty, Type titleType, LambdaExpression value)
            {
                Path = path;
                RootProperty = rootProperty;
                TitleType = titleType;
                Value = value;
            }

            public string Path { get; private set; }
            public PropertyInfo RootProperty { get; private set; }
            public Type TitleType { get; private set; }
            public LambdaExpression Value { get; private set; }
        }

        private class CustomFieldInfo
        {
            public CustomFieldInfo(string path, PropertyInfo rootProperty, Type titleType, Expression value)
            {
                Path = path;
                RootProperty = rootProperty;
                TitleType = titleType;
                Value = value;
            }

            public string Path { get; private set; }
            public PropertyInfo RootProperty { get; private set; }
            public Type TitleType { get; private set; }
            public Expression Value { get; private set; }
        }

        private class HashtableSlot
        {
            public ModelConfigurationNode ConverterTree { get; set; }
            public ModelConfigurationNode ValidationsTree { get; set; }
            public Func<TSource, TDest> Converter { get; set; }
            public Action<TSource, TDest> Merger { get; set; }

            public Hashtable ValidationMutatorsTrees { get; set; }
        }
    }
}