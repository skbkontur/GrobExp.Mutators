using System.Globalization;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class DecimalConverter
    {
        public DecimalConverter(string format)
        {
            this.format = format;
        }

        public string ToString(decimal? d)
        {
            return d?.ToString(format, CultureInfo.InvariantCulture);
        }

        public decimal? ToDecimal(string s)
        {
            return decimal.TryParse(s, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : (decimal?)null;
        }

        private const NumberStyles numberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
        private readonly string format;
    }
}