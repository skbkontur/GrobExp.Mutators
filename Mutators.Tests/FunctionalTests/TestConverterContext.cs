using GrobExp.Mutators;

namespace Mutators.Tests.FunctionalTests
{
    public class TestConverterContext : MutatorsContext
    {
        public TestConverterContext()
            : this(MutatorsContextType.None)
        {
        }

        public TestConverterContext(MutatorsContextType mutatorsContextType)
        {
            MutatorsContextType = mutatorsContextType;
        }

        public MutatorsContextType MutatorsContextType { get; }

        public override string GetKey()
        {
            return MutatorsContextType.ToString();
        }
    }
}