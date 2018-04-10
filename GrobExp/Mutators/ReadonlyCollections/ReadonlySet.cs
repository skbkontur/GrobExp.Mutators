using System;
using System.Collections;
using System.Collections.Generic;

namespace GrobExp.Mutators.ReadonlyCollections
{
    public static class ReadonlySet
    {
        public static IReadonlySet Create(string[] keys)
        {
            return new Impl(ReadonlyHashtable.Create(keys, new int[keys.Length]));
        }

        private class Impl : IReadonlySet
        {
            public Impl(IReadonlyHashtable<int> readonlyHashtable)
            {
                this.readonlyHashtable = readonlyHashtable;
            }

            public IEnumerator<string> GetEnumerator()
            {
                foreach (var kvp in readonlyHashtable)
                    yield return kvp.Key;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool ContainsKey(string key)
            {
                return readonlyHashtable.ContainsKey(key);
            }

            public void ForEach(Action<string> action)
            {
                foreach (var kvp in readonlyHashtable)
                    action(kvp.Key);
            }

            private readonly IReadonlyHashtable<int> readonlyHashtable;
        }
    }
}