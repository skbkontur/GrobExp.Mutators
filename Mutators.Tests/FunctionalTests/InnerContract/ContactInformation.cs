using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class ContactInformation
    {
        [CustomField]
        public string Name { get; set; }

        [CustomField]
        public string Phone { get; set; }

        public string Function { get; set; }
    }
}