namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class GoodItemWithAdditionalInfo : GoodItemBase
    {
        public bool IsReturnableContainer { get; set; }

        public string Name { get; set; }

        public string TypeOfUnit { get; set; }
    }
}