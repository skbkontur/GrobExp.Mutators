using System.Collections.Generic;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class DateTimeConvertersCollection
    {
        public DateTimeConvertersCollection()
        {
            converterByFormatCode = new Dictionary<string, IDateTimeConverter>
                {
                    {"UNB", new UNBDateTimeConverter()},
                    {"102", new DateTimeConverter("yyyyMMdd")},
                    {"718", new DateTimeRangeConverter("yyyyMMdd")},
                    {"713", new DateTimeRangeConverter("yyyyMMddHHmm")},
                    {"203", new DateTimeConverter("yyyyMMddHHmm")},
                    {"401", new DateTimeConverter("HHmm")}
                };
        }

        public IDateTimeConverter GetByFormatCode(string formatCode)
        {
            return string.IsNullOrEmpty(formatCode) || !converterByFormatCode.TryGetValue(formatCode, out var result) ? new EmptyDateTimeConverter() : result;
        }

        private readonly Dictionary<string, IDateTimeConverter> converterByFormatCode;
    }
}