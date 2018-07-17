using System;
using System.Collections.Concurrent;

using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators
{
    public static class StaticMultiLanguageTextCache
    {
        public static T Get<T>() where T : StaticMultiLanguageTextBase
        {
            return (T)Get(typeof(T));
        }

        public static StaticMultiLanguageTextBase Get(Type type)
        {
            return cache.GetOrAdd(type, t => (StaticMultiLanguageTextBase)Activator.CreateInstance(t));
        }

        private static readonly ConcurrentDictionary<Type, StaticMultiLanguageTextBase> cache = new ConcurrentDictionary<Type, StaticMultiLanguageTextBase>();
    }
}