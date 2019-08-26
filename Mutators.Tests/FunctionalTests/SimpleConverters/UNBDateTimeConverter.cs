using System;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class UNBDateTimeConverter : IDateTimeConverter
    {
        public UNBDateTimeConverter()
        {
            dateTimeCYMD = new DateTimeConverter("yyyyMMddHHmm");
            dateTimeYMD = new DateTimeConverter("yyMMddHHmm");
        }

        public DateTime? ToDateTime(string date)
        {
            return dateTimeYMD.ToDateTime(date) ?? dateTimeCYMD.ToDateTime(date);
        }

        public string ToString(DateTime? date)
        {
            return dateTimeCYMD.ToString(date);
        }

        private readonly DateTimeConverter dateTimeCYMD;
        private readonly DateTimeConverter dateTimeYMD;
    }
}