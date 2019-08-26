using System;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class EmptyDateTimeConverter : IDateTimeConverter
    {
        public DateTime? ToDateTime(string date)
        {
            return null;
        }

        public string ToString(DateTime? date)
        {
            return null;
        }
    }
}