namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public interface IReferenceArrayContainer<TSG>
    {
        TSG[] References { get; set; }
    }
}