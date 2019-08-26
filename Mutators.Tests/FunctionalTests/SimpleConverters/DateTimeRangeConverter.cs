using System;
using System.Globalization;

using Mutators.Tests.FunctionalTests.InnerContract;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class DateTimeRangeConverter : IDateTimeConverter
    {
        public DateTimeRangeConverter(string formatString)
        {
            this.formatString = formatString;
        }

        public DateTime? ToDateTime(string date)
        {
            return null;
        }

        public string ToString(DateTime? date)
        {
            return null;
        }

        public string ToString(DateTimeRange dateTimeRange)
        {
            if (dateTimeRange?.StartDate == null || !dateTimeRange.EndDate.HasValue)
                return null;

            string ToString(DateTime dateTime) => dateTime.ToUniversalTime().ToString(formatString, CultureInfo.InvariantCulture);

            return $"{ToString(dateTimeRange.StartDate.Value)}-{ToString(dateTimeRange.EndDate.Value)}";
        }

        private readonly string formatString;
    }
}