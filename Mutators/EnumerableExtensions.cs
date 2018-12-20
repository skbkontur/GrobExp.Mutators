using System.Collections.Generic;

using JetBrains.Annotations;

namespace GrobExp.Mutators
{
    public static class EnumerableExtensions
    {
        [NotNull]
        public static IEnumerable<T> DistinctPreserveOrder<T>([NotNull] this IEnumerable<T> enumerable)
        {
            var uniqueElements = new HashSet<T>();
            foreach (var parameter in enumerable)
            {
                if (uniqueElements.Contains(parameter))
                    continue;
                uniqueElements.Add(parameter);
                yield return parameter;
            }
        }
    }
}