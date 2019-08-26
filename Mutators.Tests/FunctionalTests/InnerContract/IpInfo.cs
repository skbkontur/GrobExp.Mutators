using GrobExp.Mutators;
using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class IpInfo
    {
        [KeyLeaf, CustomField]
        public string Inn { get; set; }

        [CustomField]
        public string FirstName { get; set; }

        [CustomField]
        public string MiddleName { get; set; }

        [CustomField]
        public string LastName { get; set; }

        [KeyLeaf, CustomField]
        public string OKPOCode { get; set; }
    }
}