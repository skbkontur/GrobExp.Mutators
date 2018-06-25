using System;
using System.Collections;
using System.Linq;

namespace GrobExp.Mutators
{
    public class PathFormatterCollection : IPathFormatterCollection
    {
        public IPathFormatter GetPathFormatter<T>()
        {
            var type = typeof(T);
            var result = (IPathFormatter)hashtable[type];
            if (result == null)
            {
                lock (lockObject)
                {
                    result = (IPathFormatter)hashtable[type];
                    if (result == null)
                    {
                        var attribute = type.GetCustomAttributes(typeof(PathFormatterAttribute), true).Cast<PathFormatterAttribute>().SingleOrDefault();
                        if (attribute == null)
                            result = defaultPathFormatter;
                        else
                            result = (IPathFormatter)Activator.CreateInstance(attribute.PathFormatterType);
                        hashtable[type] = result;
                    }
                }
            }

            return result;
        }

        private readonly IPathFormatter defaultPathFormatter = new SimplePathFormatter();
        private readonly Hashtable hashtable = new Hashtable();
        private readonly object lockObject = new object();
    }
}