using System;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public interface IDateTimeConverter
    {
        DateTime? ToDateTime(string date);
        string ToString(DateTime? date);
    }
}