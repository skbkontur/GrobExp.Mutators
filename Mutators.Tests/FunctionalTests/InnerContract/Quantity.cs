using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class Quantity
    {
        [CustomField]
        public decimal? Value { get; set; }

        [CustomField]
        public string MeasurementUnitCode { get; set; }
    }
}