namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public class SG1 : IReferenceContainer, IDtmArrayContainer
    {
        public Reference Reference { get; set; }

        public DateTimePeriod[] DateTimePeriod { get; set; }
    }
}