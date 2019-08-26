namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public interface IContainsPartyIndentifiers
    {
        string Gln { get; set; }
        string SupplierCodeInBuyerSystem { get; set; }
    }
}