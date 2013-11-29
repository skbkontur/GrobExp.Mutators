using System;

namespace GrobExp.Mutators.Exceptions
{
    public class FoundExternalDependencyException : Exception
    {
        public FoundExternalDependencyException(string format, params object[] parameters)
            : base(string.Format(format, parameters))
        {
        }
    }
}