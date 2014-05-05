using System.Collections;
using System.Collections.Generic;

namespace GrobExp.Mutators.CustomFields
{
    public class CustomFieldsContainer : IDictionary<string, CustomFieldValue>
    {
        public bool ContainsKey(string key)
        {
            return dict.ContainsKey(key);
        }

        public void Add(string key, CustomFieldValue value)
        {
            dict.Add(key, value);
        }

        public bool Remove(string key)
        {
            return dict.Remove(key);
        }

        public bool TryGetValue(string key, out CustomFieldValue value)
        {
            return dict.TryGetValue(key, out value);
        }

        public CustomFieldValue this[string name]
        {
            get
            {
                CustomFieldValue result;
                return dict.TryGetValue(name, out result) ? result : null;
            }
            set
            {
                if(dict.ContainsKey(name)) dict[name] = value;
                else dict.Add(name, value);
            }
        }

        public ICollection<string> Keys { get { return dict.Keys; } }
        public ICollection<CustomFieldValue> Values { get { return dict.Values; } }

        public void Add(KeyValuePair<string, CustomFieldValue> item)
        {
            dict.Add(item);
        }

        public void Clear()
        {
            dict.Clear();
        }

        public bool Contains(KeyValuePair<string, CustomFieldValue> item)
        {
            return dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, CustomFieldValue>[] array, int arrayIndex)
        {
            dict.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, CustomFieldValue> item)
        {
            return dict.Remove(item);
        }

        public int Count { get { return dict.Count; } }
        public bool IsReadOnly { get { return dict.IsReadOnly; } }

        public IEnumerator<KeyValuePair<string, CustomFieldValue>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private readonly IDictionary<string, CustomFieldValue> dict = new Dictionary<string, CustomFieldValue>();
    }
}