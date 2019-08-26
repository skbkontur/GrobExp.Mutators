using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class ForeignAddressInfo
    {
        [CustomField]
        public string CountryCode { get; set; }

        [CustomField]
        public string Address { get; set; }
    }
}