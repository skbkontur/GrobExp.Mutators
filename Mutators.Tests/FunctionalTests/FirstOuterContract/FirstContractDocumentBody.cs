namespace Mutators.Tests.FunctionalTests.FirstOuterContract
{
    public class FirstContractDocumentBody
    {
        public string Status { get; set; }

        public DocumentIdentificator OriginOrder { get; set; }

        public string DeliveryType { get; set; }

        public FirstContractPartyInfo Seller { get; set; }

        public FirstContractPartyInfo Buyer { get; set; }

        public DeliveryInfo DeliveryInfo { get; set; }

        public string Comment { get; set; }

        public string IntervalLength { get; set; }

        public LineItems LineItems { get; set; }

        public string[] MessageCodes { get; set; }
    }
}