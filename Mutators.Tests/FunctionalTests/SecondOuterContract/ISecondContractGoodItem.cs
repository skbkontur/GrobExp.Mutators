namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public interface ISecondContractGoodItem
    {
        LineItem LineItem { get; set; }
        AdditionalProductId[] AdditionalProductId { get; set; }
        ItemDescription[] ItemDescription { get; set; }
    }
}