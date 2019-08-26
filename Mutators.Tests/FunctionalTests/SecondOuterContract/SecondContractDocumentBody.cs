using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public class SecondContractDocumentBody : IDtmArrayContainer, IMonetaryAmountsArrayContainer
    {
        public BeginningOfMessage BeginningOfMessage { get; set; }

        public DateTimePeriod[] DateTimePeriod { get; set; }

        public FreeText[] FreeText { get; set; }

        public SG1[] References { get; set; }

        public SG2[] PartiesArray { get; set; }

        public SG10[] SG10 { get; set; }

        public SG13[] SG13 { get; set; }

        public SG28[] SG28 { get; set; }

        public MonetaryAmount[] MonetaryAmount { get; set; }

        public ControlTotal[] ControlTotal { get; set; }

        public Currencies Currency { get; set; }
    }
}