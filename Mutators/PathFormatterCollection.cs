using System;
using System.Collections;
using System.Linq;

using JetBrains.Annotations;

namespace GrobExp.Mutators
{
    public class PathFormatterCollection : IPathFormatterCollection
    {
        [NotNull]
        public IPathFormatter GetPathFormatter<TType>()
        {
            var type = typeof(TType);
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

        public void Register<TType, TPathFormatter>([NotNull] TPathFormatter pathFormatter)
            where TPathFormatter : IPathFormatter
        {
            var type = typeof(TType);
            lock (lockObject)
            {
                hashtable[type] = pathFormatter;
            }
        }

        private readonly IPathFormatter defaultPathFormatter = new SimplePathFormatter();
        private readonly Hashtable hashtable = new Hashtable();
        private readonly object lockObject = new object();
    }
}