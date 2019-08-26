using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class RussianAddressInfo
    {
        [CustomField]
        public string PostalCode { get; set; }

        [CustomField]
        public string RegionCode { get; set; }

        [CustomField]
        public string District { get; set; }

        [CustomField]
        public string City { get; set; }

        [CustomField]
        public string Village { get; set; }

        [CustomField]
        public string Street { get; set; }

        [CustomField]
        public string House { get; set; }

        [CustomField]
        public string Flat { get; set; }
    }
}