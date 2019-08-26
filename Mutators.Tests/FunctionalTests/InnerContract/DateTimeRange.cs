using System;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class DateTimeRange
    {
        public DateTimeRange(DateTime? startDate, DateTime? endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        public DateTimeRange()
        {
        }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}