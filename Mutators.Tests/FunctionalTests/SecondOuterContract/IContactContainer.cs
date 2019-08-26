namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public interface IContactContainer
    {
        ContactInformation ContactInformation { get; set; }
        CommunicationContact[] CommunicationContact { get; set; }
    }
}