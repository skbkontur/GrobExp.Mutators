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
            var converterCollection = (IConverterCollection<TSource, TDest>)hashtable[key];
            if (converterCollection == null)
                throw new InvalidOperationException("Converter collection from '" + typeof(TSource) + "' to '" + typeof(TDest) + "' is not registered");
            return converterCollection;
        }

        public INewConverterCollection<TSource, TDest, TContext> Get<TSource, TDest, TContext>()
        {
            throw new NotImplementedException();
        }

        public void Register<TSource, TDest>(IConverterCollection<TSource, TDest> collection)
        {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TDest));
            hashtable[key] = collection;
        }

        private readonly Hashtable hashtable = new Hashtable();
    }
}