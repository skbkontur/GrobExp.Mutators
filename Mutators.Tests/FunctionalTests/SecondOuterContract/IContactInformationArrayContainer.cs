namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public interface IContactInformationArrayContainer<TSG>
    {
        TSG[] ContactInformationArray { get; set; }
    }
}