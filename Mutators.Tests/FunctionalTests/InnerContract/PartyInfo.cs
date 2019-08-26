using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class PartyInfo : IContainsPartyIndentifiers, IContainsRussianPartyInfo
    {
        [CustomField]
        public string Gln { get; set; }

        [CustomField]
        public PartyAddress PartyAddress { get; set; }

        public PartyInfoType? PartyInfoType { get; set; }

        [CustomField]
        public RussianPartyInfo RussianPartyInfo { get; set; }

        public ForeignPartyInfo ForeignPartyInfo { get; set; }

        public BankAccount BankAccount { get; set; }

        [CustomField]
        public string SupplierCodeInBuyerSystem { get; set; }

        [CustomField]
        public bool UsesSimplifiedTaxSystem { get; set; }

        [CustomField]
        public ContactInformation Chief { get; set; }

        [CustomField]
        public ContactInformation OrderContact { get; set; }
    }
}