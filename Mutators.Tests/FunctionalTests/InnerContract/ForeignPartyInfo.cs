using GrobExp.Mutators;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class ForeignPartyInfo
    {
        [KeyLeaf]
        public string Tin { get; set; }

        public string Name { get; set; }
    }
}