using System;
using System.Globalization;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class DateTimeConverter : IDateTimeConverter
    {
        public DateTimeConverter(string formatString)
        {
            this.formatString = formatString;
        }

        public DateTime? ToDateTime(string date)
        {
            if (!DateTime.TryParseExact(date, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result))
                return null;
            return result;
        }

        public string ToString(DateTime? date)
        {
            return !date.HasValue ? "" : date.Value.ToUniversalTime().ToString(formatString, CultureInfo.InvariantCulture);
        }

        private readonly string formatString;
    }
}