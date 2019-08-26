using System;

namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public class ReferenceInfo
    {
        public ReferenceInfo(string number, DateTime? date, string code, string dateTimePeriodFormatCode = "203")
        {
            Number = number;
            Date = date;
            Code = code;
            DateTimePeriodFormat = dateTimePeriodFormatCode;
        }

        public string Code { get; set; }
        public string DateTimePeriodFormat { get; set; }
        public string Number { get; set; }
        public DateTime? Date { get; set; }
    }
}