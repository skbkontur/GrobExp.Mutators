using System;
using System.Collections.Generic;

namespace GrobExp.Mutators.ReadonlyCollections
{
    public static class TinyHashtable
    {
        public static string[] Create(string[] values)
        {
            CheckDifferent(values);
            var hashSet = new HashSet<int>();
            for (var n = Math.Max(values.Length, 1);; ++n)
            {
                hashSet.Clear();
                var ok = true;
                foreach (var str in values)
                {
                    var idx = (int)(((uint)str.GetHashCode()) % n);
                    if (hashSet.Contains(idx))
                    {
                        ok = false;
                        break;
                    }

                    hashSet.Add(idx);
                }

                if (ok)
                {
                    var result = new string[n];
                    foreach (var str in values)
                    {
                        var idx = (int)(((uint)str.GetHashCode()) % n);
                        result[idx] = str;
                    }

                    return result;
                }
            }
        }

        private static void CheckDifferent(string[] values)
        {
            var hashSet = new HashSet<string>();
            foreach (var str in values)
            {
                if (hashSet.Contains(str))
                    throw new InvalidOperationException(string.Format("Duplicate value '{0}'", str));
                hashSet.Add(str);
            }
        }
    }
}