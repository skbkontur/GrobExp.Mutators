using System;

namespace GrobExp.Mutators.Exceptions
{
    public class FoundCyclicDependencyException : Exception
    {
        public FoundCyclicDependencyException(string format, params object[] parameters)
            : base(string.Format(format, parameters))
        {
        }
    }
}