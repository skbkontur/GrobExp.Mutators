using System;

namespace GrobExp.Mutators.Exceptions
{
    public class PriorityOutOfRangeException: Exception
    {
        public PriorityOutOfRangeException(string message)
            : base(message)
        {
        }
    }
}