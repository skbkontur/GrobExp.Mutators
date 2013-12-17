using System;
using System.Collections;

using GrobExp.Mutators;

namespace Mutators.Tests
{
    public class TestConverterCollectionFactory : IConverterCollectionFactory
    {
        public IConverterCollection<TSource, TDest> Get<TSource, TDest>()
        {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TDest));
            return (IConverterCollection<TSource, TDest>)hashtable[key];
        }

        public void Register<TSource, TDest>(IConverterCollection<TSource, TDest> collection)
        {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TDest));
            hashtable[key] = collection;
        }

        private readonly Hashtable hashtable = new Hashtable();
    }
}