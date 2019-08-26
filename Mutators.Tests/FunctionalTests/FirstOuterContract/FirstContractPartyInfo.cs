namespace Mutators.Tests.FunctionalTests.FirstOuterContract
{
    public class FirstContractPartyInfo
    {
        public string Gln { get; set; }

        public ForeignOrganization ForeignOrganization { get; set; }

        public Organization Organization { get; set; }

        public SelfEmployed SelfEmployed { get; set; }

        public RussianAddress RussianAddress { get; set; }

        public ForeignAddress ForeignAddress { get; set; }

        public string TaxSystem { get; set; }

        public AdditionalInfo AdditionalInfo { get; set; }

        public ContactInfo ContactInfo { get; set; }
    }
}