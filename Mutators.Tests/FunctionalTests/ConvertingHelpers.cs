using System;

namespace Mutators.Tests.FunctionalTests
{
    public static class ConvertingHelpers
    {
        public static int? ParseNullableInt(this string value)
        {
            return int.TryParse(value, out var result) ? result : null as int?;
        }

        public static TEnum? ParseNullableEnum<TEnum>(this string value)
            where TEnum : struct
        {
            if (value != null && Enum.TryParse(value, out TEnum result))
                return result;
            return null;
        }
    }
}