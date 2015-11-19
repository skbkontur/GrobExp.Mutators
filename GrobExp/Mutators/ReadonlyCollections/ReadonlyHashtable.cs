using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.ReadonlyCollections
{
    public static class ReadonlyHashtable
    {
        public static IReadonlyHashtable<T> Create<T>(string[] keys, T[] values)
        {
            if(keys.Length != values.Length)
                throw new InvalidOperationException();
            switch(keys.Length)
            {
            case 0:
                return new ReadonlyHashtableWithNoChildren<T>();
            case 1:
                return new ReadonlyHashtableWithOneKey<T>(keys, values);
            case 2:
                return new ReadonlyHashtableWithTwoKeys<T>(keys, values);
            case 3:
                return new ReadonlyHashtableWithThreeKeys<T>(keys, values);
            case 4:
                return new ReadonlyHashtableWithFourKeys<T>(keys, values);
            case 5:
                return new ReadonlyHashtableWithFiveKeys<T>(keys, values);
            case 6:
                return new ReadonlyHashtableWithSixKeys<T>(keys, values);
            case 7:
                return new ReadonlyHashtableWithSevenKeys<T>(keys, values);
            default:
                return new ReadonlyHashtableWithArbitraryNumberOfKeys<T>(keys, values);
            }
        }

        public static IReadonlyHashtable<T> Create<TFrom, T>(Dictionary<string, TFrom> dict, Func<TFrom, T> selector)
        {
            return Create(dict.Keys.ToArray(), dict.Values.Select(selector).ToArray());
        }

        public static IReadonlyHashtable<T> Create<TFrom, T>(Dictionary<string, TFrom> dict, Func<string, TFrom, T> selector)
        {
            return Create(dict.Keys.ToArray(), dict.Select(pair => selector(pair.Key, pair.Value)).ToArray());
        }

        public static IReadonlyHashtable<T> Create<T>(Dictionary<string, T> dict)
        {
            return Create(dict.Keys.ToArray(), dict.Values.ToArray());
        }

        #region impl

        private class ReadonlyHashtableWithArbitraryNumberOfKeys<T> : IReadonlyHashtable<T>
        {
            private ReadonlyHashtableWithArbitraryNumberOfKeys(string[] keys, T[] values, int[] indexes)
            {
                this.keys = keys;
                this.values = values;
                this.indexes = indexes;
                n = this.keys.Length;
            }

            public ReadonlyHashtableWithArbitraryNumberOfKeys(string[] keys, T[] values)
            {
                this.keys = TinyHashtable.Create(keys);
                n = this.keys.Length;
                this.values = new T[n];
                indexes = new int[keys.Length];
                for(var i = 0; i < keys.Length; ++i)
                {
                    var key = keys[i];
                    var idx = (int)(((uint)key.GetHashCode()) % n);
                    this.values[idx] = values[i];
                    indexes[i] = idx;
                }
            }

            public bool TryGetValue(string key, out T value)
            {
                var idx = (int)(((uint)key.GetHashCode()) % n);
                if(keys[idx] == key)
                {
                    value = values[idx];
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                var idx = (int)(((uint)key.GetHashCode()) % n);
                if(keys[idx] == key)
                {
                    values[idx] = value;
                    return true;
                }
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                var idx = (int)(((uint)key.GetHashCode()) % n);
                if(keys[idx] == key)
                {
                    if(updateFilter == null || updateFilter(values[idx]))
                        values[idx] = valueFactory();
                    value = values[idx];
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                var idx = (int)(((uint)key.GetHashCode()) % n);
                return keys[idx] == key;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                var newValues = new TTo[values.Length];
                for(int i = 0; i < newValues.Length; ++i)
                    newValues[i] = selector(values[i]);
                return new ReadonlyHashtableWithArbitraryNumberOfKeys<TTo>(keys, newValues, indexes);
            }

            public void ForEach(Action<T> action)
            {
                foreach(var idx in indexes)
                    action(values[idx]);
            }

            public void ForEach(Action<string, T> action)
            {
                foreach(var idx in indexes)
                    action(keys[idx], values[idx]);
            }

            public void Clear()
            {
                Array.Clear(values, 0, values.Length);
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                foreach(var idx in indexes)
                    yield return new KeyValuePair<string, T>(keys[idx], values[idx]);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private readonly int[] indexes;
            private readonly string[] keys;
            private readonly int n;
            private readonly T[] values;
        }

        private class ReadonlyHashtableWithNoChildren<T> : IReadonlyHashtable<T>
        {
            public bool TryGetValue(string key, out T value)
            {
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                return false;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                return new ReadonlyHashtableWithNoChildren<TTo>();
            }

            public void ForEach(Action<T> action)
            {
            }

            public void ForEach(Action<string, T> action)
            {
            }

            public void Clear()
            {
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                return Enumerable.Empty<KeyValuePair<string, T>>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ReadonlyHashtableWithOneKey<T> : IReadonlyHashtable<T>
        {
            public ReadonlyHashtableWithOneKey(string[] keys, T[] values)
            {
                key0 = keys[0];
                value0 = values[0];
            }

            public bool TryGetValue(string key, out T value)
            {
                if(key == key0)
                {
                    value = value0;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                if(key == key0)
                {
                    value0 = value;
                    return true;
                }
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                if(key == key0)
                {
                    if(updateFilter == null || updateFilter(value0))
                        value0 = valueFactory();
                    value = value0;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                return key == key0;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                return new ReadonlyHashtableWithOneKey<TTo>(new[] {key0}, new[] {selector(value0)});
            }

            public void ForEach(Action<T> action)
            {
                action(value0);
            }

            public void ForEach(Action<string, T> action)
            {
                action(key0, value0);
            }

            public void Clear()
            {
                value0 = default(T);
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                yield return new KeyValuePair<string, T>(key0, value0);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private readonly string key0;
            private T value0;
        }

        private class ReadonlyHashtableWithTwoKeys<T> : IReadonlyHashtable<T>
        {
            public ReadonlyHashtableWithTwoKeys(string[] keys, T[] values)
            {
                key0 = keys[0];
                key1 = keys[1];
                value0 = values[0];
                value1 = values[1];
            }

            public bool TryGetValue(string key, out T value)
            {
                if(key == key0)
                {
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    value = value1;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                if(key == key0)
                {
                    value0 = value;
                    return true;
                }
                if(key == key1)
                {
                    value1 = value;
                    return true;
                }
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                if(key == key0)
                {
                    if(updateFilter == null || updateFilter(value0))
                        value0 = valueFactory();
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    if(updateFilter == null || updateFilter(value1))
                        value1 = valueFactory();
                    value = value1;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                return key == key0 || key == key1;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                return new ReadonlyHashtableWithTwoKeys<TTo>(new[] {key0, key1}, new[] {selector(value0), selector(value1)});
            }

            public void ForEach(Action<T> action)
            {
                action(value0);
                action(value1);
            }

            public void ForEach(Action<string, T> action)
            {
                action(key0, value0);
                action(key1, value1);
            }

            public void Clear()
            {
                value0 = default(T);
                value1 = default(T);
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                yield return new KeyValuePair<string, T>(key0, value0);
                yield return new KeyValuePair<string, T>(key1, value1);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private readonly string key0;
            private readonly string key1;
            private T value0;
            private T value1;
        }

        private class ReadonlyHashtableWithThreeKeys<T> : IReadonlyHashtable<T>
        {
            public ReadonlyHashtableWithThreeKeys(string[] keys, T[] values)
            {
                key0 = keys[0];
                key1 = keys[1];
                key2 = keys[2];
                value0 = values[0];
                value1 = values[1];
                value2 = values[2];
            }

            public bool TryGetValue(string key, out T value)
            {
                if(key == key0)
                {
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    value = value2;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                if(key == key0)
                {
                    value0 = value;
                    return true;
                }
                if(key == key1)
                {
                    value1 = value;
                    return true;
                }
                if(key == key2)
                {
                    value2 = value;
                    return true;
                }
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                if(key == key0)
                {
                    if(updateFilter == null || updateFilter(value0))
                        value0 = valueFactory();
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    if(updateFilter == null || updateFilter(value1))
                        value1 = valueFactory();
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    if(updateFilter == null || updateFilter(value2))
                        value2 = valueFactory();
                    value = value2;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                return key == key0 || key == key1 || key == key2;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                return new ReadonlyHashtableWithThreeKeys<TTo>(new[] {key0, key1, key2},
                                                               new[] {selector(value0), selector(value1), selector(value2)});
            }

            public void ForEach(Action<T> action)
            {
                action(value0);
                action(value1);
                action(value2);
            }

            public void ForEach(Action<string, T> action)
            {
                action(key0, value0);
                action(key1, value1);
                action(key2, value2);
            }

            public void Clear()
            {
                value0 = default(T);
                value1 = default(T);
                value2 = default(T);
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                yield return new KeyValuePair<string, T>(key0, value0);
                yield return new KeyValuePair<string, T>(key1, value1);
                yield return new KeyValuePair<string, T>(key2, value2);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private readonly string key0;
            private readonly string key1;
            private readonly string key2;
            private T value0;
            private T value1;
            private T value2;
        }

        private class ReadonlyHashtableWithFourKeys<T> : IReadonlyHashtable<T>
        {
            public ReadonlyHashtableWithFourKeys(string[] keys, T[] values)
            {
                key0 = keys[0];
                key1 = keys[1];
                key2 = keys[2];
                key3 = keys[3];
                value0 = values[0];
                value1 = values[1];
                value2 = values[2];
                value3 = values[3];
            }

            public bool TryGetValue(string key, out T value)
            {
                if(key == key0)
                {
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    value = value2;
                    return true;
                }
                if(key == key3)
                {
                    value = value3;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                if(key == key0)
                {
                    value0 = value;
                    return true;
                }
                if(key == key1)
                {
                    value1 = value;
                    return true;
                }
                if(key == key2)
                {
                    value2 = value;
                    return true;
                }
                if(key == key3)
                {
                    value3 = value;
                    return true;
                }
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                if(key == key0)
                {
                    if(updateFilter == null || updateFilter(value0))
                        value0 = valueFactory();
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    if(updateFilter == null || updateFilter(value1))
                        value1 = valueFactory();
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    if(updateFilter == null || updateFilter(value2))
                        value2 = valueFactory();
                    value = value2;
                    return true;
                }
                if(key == key3)
                {
                    if(updateFilter == null || updateFilter(value3))
                        value3 = valueFactory();
                    value = value3;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                return key == key0 || key == key1 || key == key2 || key == key3;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                return new ReadonlyHashtableWithFourKeys<TTo>(new[] {key0, key1, key2, key3},
                                                              new[] {selector(value0), selector(value1), selector(value2), selector(value3)});
            }

            public void ForEach(Action<T> action)
            {
                action(value0);
                action(value1);
                action(value2);
                action(value3);
            }

            public void ForEach(Action<string, T> action)
            {
                action(key0, value0);
                action(key1, value1);
                action(key2, value2);
                action(key3, value3);
            }

            public void Clear()
            {
                value0 = default(T);
                value1 = default(T);
                value2 = default(T);
                value3 = default(T);
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                yield return new KeyValuePair<string, T>(key0, value0);
                yield return new KeyValuePair<string, T>(key1, value1);
                yield return new KeyValuePair<string, T>(key2, value2);
                yield return new KeyValuePair<string, T>(key3, value3);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private readonly string key0;
            private readonly string key1;
            private readonly string key2;
            private readonly string key3;
            private T value0;
            private T value1;
            private T value2;
            private T value3;
        }

        private class ReadonlyHashtableWithFiveKeys<T> : IReadonlyHashtable<T>
        {
            public ReadonlyHashtableWithFiveKeys(string[] keys, T[] values)
            {
                key0 = keys[0];
                key1 = keys[1];
                key2 = keys[2];
                key3 = keys[3];
                key4 = keys[4];
                value0 = values[0];
                value1 = values[1];
                value2 = values[2];
                value3 = values[3];
                value4 = values[4];
            }

            public bool TryGetValue(string key, out T value)
            {
                if(key == key0)
                {
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    value = value2;
                    return true;
                }
                if(key == key3)
                {
                    value = value3;
                    return true;
                }
                if(key == key4)
                {
                    value = value4;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                if(key == key0)
                {
                    value0 = value;
                    return true;
                }
                if(key == key1)
                {
                    value1 = value;
                    return true;
                }
                if(key == key2)
                {
                    value2 = value;
                    return true;
                }
                if(key == key3)
                {
                    value3 = value;
                    return true;
                }
                if(key == key4)
                {
                    value4 = value;
                    return true;
                }
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                if(key == key0)
                {
                    if(updateFilter == null || updateFilter(value0))
                        value0 = valueFactory();
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    if(updateFilter == null || updateFilter(value1))
                        value1 = valueFactory();
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    if(updateFilter == null || updateFilter(value2))
                        value2 = valueFactory();
                    value = value2;
                    return true;
                }
                if(key == key3)
                {
                    if(updateFilter == null || updateFilter(value3))
                        value3 = valueFactory();
                    value = value3;
                    return true;
                }
                if(key == key4)
                {
                    if(updateFilter == null || updateFilter(value4))
                        value4 = valueFactory();
                    value = value4;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                return key == key0 || key == key1 || key == key2 || key == key3 || key == key4;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                return new ReadonlyHashtableWithFiveKeys<TTo>(new[] {key0, key1, key2, key3, key4},
                                                              new[] {selector(value0), selector(value1), selector(value2), selector(value3), selector(value4)});
            }

            public void ForEach(Action<T> action)
            {
                action(value0);
                action(value1);
                action(value2);
                action(value3);
                action(value4);
            }

            public void ForEach(Action<string, T> action)
            {
                action(key0, value0);
                action(key1, value1);
                action(key2, value2);
                action(key3, value3);
                action(key4, value4);
            }

            public void Clear()
            {
                value0 = default(T);
                value1 = default(T);
                value2 = default(T);
                value3 = default(T);
                value4 = default(T);
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                yield return new KeyValuePair<string, T>(key0, value0);
                yield return new KeyValuePair<string, T>(key1, value1);
                yield return new KeyValuePair<string, T>(key2, value2);
                yield return new KeyValuePair<string, T>(key3, value3);
                yield return new KeyValuePair<string, T>(key4, value4);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private readonly string key0;
            private readonly string key1;
            private readonly string key2;
            private readonly string key3;
            private readonly string key4;
            private T value0;
            private T value1;
            private T value2;
            private T value3;
            private T value4;
        }

        private class ReadonlyHashtableWithSixKeys<T> : IReadonlyHashtable<T>
        {
            public ReadonlyHashtableWithSixKeys(string[] keys, T[] values)
            {
                key0 = keys[0];
                key1 = keys[1];
                key2 = keys[2];
                key3 = keys[3];
                key4 = keys[4];
                key5 = keys[5];
                value0 = values[0];
                value1 = values[1];
                value2 = values[2];
                value3 = values[3];
                value4 = values[4];
                value5 = values[5];
            }

            public bool TryGetValue(string key, out T value)
            {
                if(key == key0)
                {
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    value = value2;
                    return true;
                }
                if(key == key3)
                {
                    value = value3;
                    return true;
                }
                if(key == key4)
                {
                    value = value4;
                    return true;
                }
                if(key == key5)
                {
                    value = value5;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                if(key == key0)
                {
                    value0 = value;
                    return true;
                }
                if(key == key1)
                {
                    value1 = value;
                    return true;
                }
                if(key == key2)
                {
                    value2 = value;
                    return true;
                }
                if(key == key3)
                {
                    value3 = value;
                    return true;
                }
                if(key == key4)
                {
                    value4 = value;
                    return true;
                }
                if(key == key5)
                {
                    value5 = value;
                    return true;
                }
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                if(key == key0)
                {
                    if(updateFilter == null || updateFilter(value0))
                        value0 = valueFactory();
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    if(updateFilter == null || updateFilter(value1))
                        value1 = valueFactory();
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    if(updateFilter == null || updateFilter(value2))
                        value2 = valueFactory();
                    value = value2;
                    return true;
                }
                if(key == key3)
                {
                    if(updateFilter == null || updateFilter(value3))
                        value3 = valueFactory();
                    value = value3;
                    return true;
                }
                if(key == key4)
                {
                    if(updateFilter == null || updateFilter(value4))
                        value4 = valueFactory();
                    value = value4;
                    return true;
                }
                if(key == key5)
                {
                    if(updateFilter == null || updateFilter(value5))
                        value5 = valueFactory();
                    value = value5;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                return key == key0 || key == key1 || key == key2 || key == key3 || key == key4 || key == key5;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                return new ReadonlyHashtableWithSixKeys<TTo>(new[] {key0, key1, key2, key3, key4, key5},
                                                             new[] {selector(value0), selector(value1), selector(value2), selector(value3), selector(value4), selector(value5)});
            }

            public void ForEach(Action<T> action)
            {
                action(value0);
                action(value1);
                action(value2);
                action(value3);
                action(value4);
                action(value5);
            }

            public void ForEach(Action<string, T> action)
            {
                action(key0, value0);
                action(key1, value1);
                action(key2, value2);
                action(key3, value3);
                action(key4, value4);
                action(key5, value5);
            }

            public void Clear()
            {
                value0 = default(T);
                value1 = default(T);
                value2 = default(T);
                value3 = default(T);
                value4 = default(T);
                value5 = default(T);
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                yield return new KeyValuePair<string, T>(key0, value0);
                yield return new KeyValuePair<string, T>(key1, value1);
                yield return new KeyValuePair<string, T>(key2, value2);
                yield return new KeyValuePair<string, T>(key3, value3);
                yield return new KeyValuePair<string, T>(key4, value4);
                yield return new KeyValuePair<string, T>(key5, value5);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private readonly string key0;
            private readonly string key1;
            private readonly string key2;
            private readonly string key3;
            private readonly string key4;
            private readonly string key5;
            private T value0;
            private T value1;
            private T value2;
            private T value3;
            private T value4;
            private T value5;
        }

        private class ReadonlyHashtableWithSevenKeys<T> : IReadonlyHashtable<T>
        {
            public ReadonlyHashtableWithSevenKeys(string[] keys, T[] values)
            {
                key0 = keys[0];
                key1 = keys[1];
                key2 = keys[2];
                key3 = keys[3];
                key4 = keys[4];
                key5 = keys[5];
                key6 = keys[6];
                value0 = values[0];
                value1 = values[1];
                value2 = values[2];
                value3 = values[3];
                value4 = values[4];
                value5 = values[5];
                value6 = values[6];
            }

            public bool TryGetValue(string key, out T value)
            {
                if(key == key0)
                {
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    value = value2;
                    return true;
                }
                if(key == key3)
                {
                    value = value3;
                    return true;
                }
                if(key == key4)
                {
                    value = value4;
                    return true;
                }
                if(key == key5)
                {
                    value = value5;
                    return true;
                }
                if(key == key6)
                {
                    value = value6;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool TryUpdateValue(string key, T value)
            {
                if(key == key0)
                {
                    value0 = value;
                    return true;
                }
                if(key == key1)
                {
                    value1 = value;
                    return true;
                }
                if(key == key2)
                {
                    value2 = value;
                    return true;
                }
                if(key == key3)
                {
                    value3 = value;
                    return true;
                }
                if(key == key4)
                {
                    value4 = value;
                    return true;
                }
                if(key == key5)
                {
                    value5 = value;
                    return true;
                }
                if(key == key6)
                {
                    value6 = value;
                    return true;
                }
                return false;
            }

            public bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value)
            {
                if(key == key0)
                {
                    if(updateFilter == null || updateFilter(value0))
                        value0 = valueFactory();
                    value = value0;
                    return true;
                }
                if(key == key1)
                {
                    if(updateFilter == null || updateFilter(value1))
                        value1 = valueFactory();
                    value = value1;
                    return true;
                }
                if(key == key2)
                {
                    if(updateFilter == null || updateFilter(value2))
                        value2 = valueFactory();
                    value = value2;
                    return true;
                }
                if(key == key3)
                {
                    if(updateFilter == null || updateFilter(value3))
                        value3 = valueFactory();
                    value = value3;
                    return true;
                }
                if(key == key4)
                {
                    if(updateFilter == null || updateFilter(value4))
                        value4 = valueFactory();
                    value = value4;
                    return true;
                }
                if(key == key5)
                {
                    if(updateFilter == null || updateFilter(value5))
                        value5 = valueFactory();
                    value = value5;
                    return true;
                }
                if(key == key6)
                {
                    if(updateFilter == null || updateFilter(value6))
                        value6 = valueFactory();
                    value = value6;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool ContainsKey(string key)
            {
                return key == key0 || key == key1 || key == key2 || key == key3 || key == key4 || key == key5 || key == key6;
            }

            public IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector)
            {
                return new ReadonlyHashtableWithSevenKeys<TTo>(new[] {key0, key1, key2, key3, key4, key5, key6},
                                                               new[] {selector(value0), selector(value1), selector(value2), selector(value3), selector(value4), selector(value5), selector(value6)});
            }

            public void ForEach(Action<T> action)
            {
                action(value0);
                action(value1);
                action(value2);
                action(value3);
                action(value4);
                action(value5);
                action(value6);
            }

            public void ForEach(Action<string, T> action)
            {
                action(key0, value0);
                action(key1, value1);
                action(key2, value2);
                action(key3, value3);
                action(key4, value4);
                action(key5, value5);
                action(key6, value6);
            }

            public void Clear()
            {
                value0 = default(T);
                value1 = default(T);
                value2 = default(T);
                value3 = default(T);
                value4 = default(T);
                value5 = default(T);
                value6 = default(T);
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                yield return new KeyValuePair<string, T>(key0, value0);
                yield return new KeyValuePair<string, T>(key1, value1);
                yield return new KeyValuePair<string, T>(key2, value2);
                yield return new KeyValuePair<string, T>(key3, value3);
                yield return new KeyValuePair<string, T>(key4, value4);
                yield return new KeyValuePair<string, T>(key5, value5);
                yield return new KeyValuePair<string, T>(key6, value6);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private readonly string key0;
            private readonly string key1;
            private readonly string key2;
            private readonly string key3;
            private readonly string key4;
            private readonly string key5;
            private readonly string key6;
            private T value0;
            private T value1;
            private T value2;
            private T value3;
            private T value4;
            private T value5;
            private T value6;
        }

        #endregion
    }
}