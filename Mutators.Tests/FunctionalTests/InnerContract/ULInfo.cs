using GrobExp.Mutators;
using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class UlInfo
    {
        [KeyLeaf, CustomField]
        public string Inn { get; set; }

        [CustomField]
        public string Name { get; set; }

        [KeyLeaf, CustomField]
        public string OKPOCode { get; set; }
    }
}