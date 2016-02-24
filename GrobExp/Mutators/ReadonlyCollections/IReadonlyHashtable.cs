using System;
using System.Collections.Generic;

namespace GrobExp.Mutators.ReadonlyCollections
{
	public interface IReadonlyHashtable<T> : IEnumerable<KeyValuePair<string, T>>
	{
		bool TryGetValue(string key, out T value);
		bool TryUpdateValue(string key, T value);
		bool TryUpdateValue(string key, Func<T, bool> updateFilter, Func<T> valueFactory, out T value);
		void ForEach(Action<T> action);
		void ForEach(Action<string, T> action);
		void Clear();
		bool ContainsKey(string key);
		IReadonlyHashtable<TTo> Clone<TTo>(Func<T, TTo> selector);
	}
}