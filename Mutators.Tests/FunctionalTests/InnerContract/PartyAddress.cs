using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class PartyAddress : IContainsRussianPartyAdress, IContainsAddressType
    {
        [CustomField]
        public AddressType AddressType { get; set; }

        [CustomField]
        public RussianAddressInfo RussianAddressInfo { get; set; }

        [CustomField]
        public ForeignAddressInfo ForeignAddressInfo { get; set; }
    }
}