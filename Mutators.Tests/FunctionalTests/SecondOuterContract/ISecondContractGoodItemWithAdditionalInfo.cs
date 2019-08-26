namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public interface ISecondContractGoodItemWithAdditionalInfo : ISecondContractGoodItem
    {
        AdditionalInformation[] AdditionalInformation { get; set; }
    }
}