using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public class SG28 : ISecondContractGoodItemWithAdditionalInfo, IMonetaryAmountsArrayContainer
    {
        public LineItem LineItem { get; set; }

        public AdditionalProductId[] AdditionalProductId { get; set; }

        public ItemDescription[] ItemDescription { get; set; }

        public Quantity[] Quantity { get; set; }

        public AdditionalInformation[] AdditionalInformation { get; set; }

        public DateTimePeriod[] DateTimePeriod { get; set; }

        public MonetaryAmount[] MonetaryAmount { get; set; }

        public GoodsIdentityNumber[] GoodsIdentityNumber { get; set; }

        public FreeText[] FreeText { get; set; }

        public SG34[] SG34 { get; set; }
    }
}