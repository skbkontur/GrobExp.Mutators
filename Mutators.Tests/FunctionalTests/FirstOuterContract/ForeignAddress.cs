namespace Mutators.Tests.FunctionalTests.FirstOuterContract
{
    public class ForeignAddress
    {
        public string CountryIsoCode { get; set; }

        public string Address { get; set; }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(CountryIsoCode) && string.IsNullOrEmpty(Address);
        }
    }
}