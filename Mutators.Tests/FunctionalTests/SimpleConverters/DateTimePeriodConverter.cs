using System;

using Mutators.Tests.FunctionalTests.SecondOuterContract;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class DateTimePeriodConverter
    {
        public DateTimePeriodConverter(DateTimeConvertersCollection dateTimeConvertersCollection)
        {
            this.dateTimeConvertersCollection = dateTimeConvertersCollection;
        }

        public DateTime? ToDateTime(DateTimePeriodGroup dateTimePeriodGroup)
        {
            if (dateTimePeriodGroup == null || string.IsNullOrEmpty(dateTimePeriodGroup.Value))
                return null;
            return dateTimeConvertersCollection.GetByFormatCode(dateTimePeriodGroup.FormatCode).ToDateTime(dateTimePeriodGroup.Value);
        }

        public DateTimePeriod ToDateTimePeriod(DateTime? dateTime, string functionCodeQualifier, string formatCode)
        {
            if (!dateTime.HasValue)
                return null;
            return new DateTimePeriod
                {
                    DateTimePeriodGroup = new DateTimePeriodGroup
                        {
                            FormatCode = formatCode,
                            FunctionCodeQualifier = functionCodeQualifier,
                            Value = dateTimeConvertersCollection.GetByFormatCode(formatCode).ToString(dateTime)
                        }
                };
        }

        private readonly DateTimeConvertersCollection dateTimeConvertersCollection;
    }
}