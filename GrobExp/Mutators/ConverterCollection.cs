using System;
using System.Collections;
using System.Linq.Expressions;

using GrobExp.Compiler;

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

        private void ConfigureInternal(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator)
        {
            Configure(context, configurator);
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