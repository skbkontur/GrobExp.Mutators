using System;

using Mutators.Tests.FunctionalTests.SecondOuterContract;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public static class StaticDateTimePeriodConverter
    {
        public static DateTime ToDateTimeOrUtcNow(DateTimePeriodGroup dateTimePeriodGroup)
        {
            return DateTimePeriodConverter.ToDateTime(dateTimePeriodGroup) ?? DateTime.UtcNow;
        }

        public static DateTime? ToDateTime(DateTimePeriodGroup dateTimePeriodGroup)
        {
            return DateTimePeriodConverter.ToDateTime(dateTimePeriodGroup);
        }

        public static DateTimePeriod ToDateTimePeriod(DateTime? dateTime, string functionCodeQualifier, string formatCode)
        {
            return DateTimePeriodConverter.ToDateTimePeriod(dateTime, functionCodeQualifier, formatCode);
        }

        private static DateTimePeriodConverter DateTimePeriodConverter { get; } = new DateTimePeriodConverter(new DateTimeConvertersCollection());
    }
}