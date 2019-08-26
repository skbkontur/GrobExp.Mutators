using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class DespatchPartyInfo
    {
        [CustomField]
        public PartyInfo PartyInfo { get; set; }
    }
}