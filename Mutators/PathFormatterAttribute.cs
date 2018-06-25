using System;

namespace GrobExp.Mutators
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PathFormatterAttribute : Attribute
    {
        public PathFormatterAttribute(Type pathFormatterType)
        {
            PathFormatterType = pathFormatterType;
        }

        public Type PathFormatterType { get; private set; }
    }
}