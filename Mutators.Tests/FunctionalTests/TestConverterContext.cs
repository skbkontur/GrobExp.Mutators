using GrobExp.Mutators;

namespace Mutators.Tests.FunctionalTests
{
    public class TestConverterContext : MutatorsContext
    {
        public MutatorsContextType MutatorsContextType { get; }

        public TestConverterContext() : this(MutatorsContextType.None)
        {
        }
        
        public TestConverterContext(MutatorsContextType mutatorsContextType)
        {
            MutatorsContextType = mutatorsContextType;
        }

        public override string GetKey()
        {
            return MutatorsContextType.ToString();
        }
    }
}