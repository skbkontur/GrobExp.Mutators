using System.Collections;

using GrobExp.Mutators;

namespace Mutators.Tests
{
    public class TestDataConfiguratorCollectionFactory : IDataConfiguratorCollectionFactory
    {
        public IDataConfiguratorCollection<T> Get<T>()
        {
            return (IDataConfiguratorCollection<T>)hashtable[typeof(T)];
        }

        public void Register<T>(IDataConfiguratorCollection<T> collection)
        {
            hashtable.Add(typeof(T), collection);
        }

        private readonly Hashtable hashtable = new Hashtable();
    }
}