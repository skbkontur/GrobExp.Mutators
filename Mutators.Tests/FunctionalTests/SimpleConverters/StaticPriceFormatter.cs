using System.Globalization;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public static class StaticPriceFormatter
    {
        public static string FormatPrice(decimal? price)
        {
            return price.HasValue ? PriceDecimalConverter.ToString(price.Value) : string.Empty;
        }

        public static decimal? Parse(string decimalString)
        {
            return decimal.TryParse(decimalString, numberStyles, CultureInfo.InvariantCulture, out var result) ? result : (decimal?)null;
        }

        private const NumberStyles numberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
        private static DecimalConverter PriceDecimalConverter { get; } = new DecimalConverter("0.0000");
    }
}