using System;
using System.Collections.Generic;

namespace GrobExp.Mutators.ReadonlyCollections
{
	public interface IReadonlySet : IEnumerable<string>
	{
		void ForEach(Action<string> action);
		bool ContainsKey(string key);
	}
}