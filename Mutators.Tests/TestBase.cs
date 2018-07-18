using NUnit.Framework;

namespace Mutators.Tests
{
    [TestFixture]
    public abstract class TestBase
    {
        [SetUp]
        protected virtual void SetUp()
        {
        }
    }
}