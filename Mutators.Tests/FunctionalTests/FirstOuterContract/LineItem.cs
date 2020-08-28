namespace Mutators.Tests.FunctionalTests.FirstOuterContract
{
    public class LineItem
    {
        public string Gtin { get; set; }

        public string TypeOfUnit { get; set; }

        public MeasureUnitQuantity OrderedQuantity { get; set; }

        public ReasonQuantity[] ToBeReturnedQuantity { get; set; }

        public string[] Declarations { get; set; }

        public string[] ControlMarks { get; set; }

        public string FlowType { get; set; }
    }
}