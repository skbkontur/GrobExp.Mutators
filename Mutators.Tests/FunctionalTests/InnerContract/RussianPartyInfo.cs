using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class RussianPartyInfo
    {
        [CustomField]
        public RussianPartyType RussianPartyType { get; set; }

        [CustomField]
        public UlInfo ULInfo { get; set; }

        [CustomField]
        public IpInfo IPInfo { get; set; }
    }
}