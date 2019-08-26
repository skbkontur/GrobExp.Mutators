namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public class NameAndAddress
    {
        public string PartyFunctionCodeQualifier { get; set; }

        public PartyIdentificationDetails PartyIdentificationDetails { get; set; }

        public NameAndAddressGroup NameAndAddressGroup { get; set; }

        public PartyNameType PartyNameType { get; set; }

        public Street Street { get; set; }

        public string CityName { get; set; }

        public CountrySubEntityDetails CountrySubEntityDetails { get; set; }

        public string PostalIdentificationCode { get; set; }

        public string CountryNameCode { get; set; }
    }
}