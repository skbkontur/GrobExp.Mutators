using System;
using System.Collections;
using System.Linq;

namespace GrobExp.Mutators
{
    public abstract class DataConfiguratorCollectionBase<TData> : IDataConfiguratorCollection<TData>
    {
        protected DataConfiguratorCollectionBase(IDataConfiguratorCollectionFactory dataConfiguratorCollectionFactory, IConverterCollectionFactory converterCollectionFactory, IPathFormatterCollection pathFormatterCollection)
        {
            this.dataConfiguratorCollectionFactory = dataConfiguratorCollectionFactory;
            this.converterCollectionFactory = converterCollectionFactory;
            this.pathFormatterCollection = pathFormatterCollection;
        }

        public MutatorsTree<TData> GetMutatorsTree(MutatorsContext context, int validationsPriority = 0)
        {
            if(context == null)
                throw new ArgumentNullException("context");
            var slot = GetOrCreateHashtableSlot(context);
            var result = (MutatorsTree<TData>)slot.MutatorsTrees[validationsPriority];
            if(result == null)
            {
                lock(lockObject)
                {
                    result = (MutatorsTree<TData>)slot.MutatorsTrees[validationsPriority];
                    if(result == null)
                        slot.MutatorsTrees[validationsPriority] = result = new SimpleMutatorsTree<TData>(slot.Tree, pathFormatterCollection.GetPathFormatter<TData>(), pathFormatterCollection, validationsPriority);
                }
            }
            return result;
        }

        public MutatorsTree<TData> GetMutatorsTree(Type[] path, MutatorsContext[] mutatorsContexts, MutatorsContext[] converterContexts)
        {
            if(path == null)
                throw new ArgumentNullException("path");
            if(mutatorsContexts == null)
                throw new ArgumentNullException("mutatorsContexts");
            if(converterContexts == null)
                throw new ArgumentNullException("converterContexts");
            var n = path.Length;
            if(mutatorsContexts.Length != n + 1)
                throw new ArgumentException("Incorrect number of mutators contexts", "mutatorsContexts");
            if(converterContexts.Length != n)
                throw new ArgumentException("Incorrect number of converter contexts", "converterContexts");
            var key = string.Join("@", path.Select(type => type.FullName));
            var slot2 = (HashtableSlot2)hashtable[key];
            if(slot2 == null)
            {
                lock(lockObject)
                {
                    slot2 = (HashtableSlot2)hashtable[key];
                    if(slot2 == null)
                    {
                        hashtable[key] = slot2 = new HashtableSlot2
                            {
                                MutatorsTreeCreator = DataConfiguratorCollectionHelper.GetOrCreateMutatorsTreeCreator<TData>(path),
                                MutatorsTrees = new Hashtable()
                            };
                    }
                }
            }
            key = string.Join("@", mutatorsContexts.Select(context => context.GetKey()).Concat(converterContexts.Select(context => context.GetKey())));
            var result = (MutatorsTree<TData>)slot2.MutatorsTrees[key];
            if(result == null)
            {
                lock(lockObject)
                {
                    result = (MutatorsTree<TData>)slot2.MutatorsTrees[key];
                    if(result == null)
                        slot2.MutatorsTrees[key] = result = slot2.MutatorsTreeCreator.GetMutatorsTree(dataConfiguratorCollectionFactory, converterCollectionFactory, mutatorsContexts, converterContexts);
                }
            }
            return result;
        }

        public void Clear()
        {
            foreach(object value in hashtable.Values)
            {
                var slot = value as HashtableSlot;
                if(slot != null)
                {
                    foreach(MutatorsTree<TData> tree in slot.MutatorsTrees.Values)
                        tree.Clear();
                    slot.Tree = null;
                }
                var slot2 = value as HashtableSlot2;
                if(slot2 != null)
                {
                    foreach (MutatorsTree<TData> tree in slot2.MutatorsTrees.Values)
                        tree.Clear();
                }
            }
        }

        protected abstract void Configure(MutatorsContext context, MutatorsConfigurator<TData> configurator);

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
                        var root = ModelConfigurationNode.CreateRoot(typeof(TData));
                        Configure(context, new MutatorsConfigurator<TData>(root));
                        hashtable[key] = slot = new HashtableSlot
                            {
                                Tree = root,
                                MutatorsTrees = new Hashtable()
                            };
                    }
                }
            }
            return slot;
        }

        private readonly IDataConfiguratorCollectionFactory dataConfiguratorCollectionFactory;
        private readonly IConverterCollectionFactory converterCollectionFactory;
        private readonly IPathFormatterCollection pathFormatterCollection;

        private readonly Hashtable hashtable = new Hashtable();
        private readonly object lockObject = new object();

        private class HashtableSlot
        {
            public ModelConfigurationNode Tree { get; set; }
            public Hashtable MutatorsTrees { get; set; }
        }

        private class HashtableSlot2
        {
            public DataConfiguratorCollectionHelper.IMutatorsTreeCreator<TData> MutatorsTreeCreator { get; set; }
            public Hashtable MutatorsTrees { get; set; }
        }
    }
}