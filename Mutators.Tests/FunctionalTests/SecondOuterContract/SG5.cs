namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public class SG5 : IContactContainer
    {
        public ContactInformation ContactInformation { get; set; }

        public CommunicationContact[] CommunicationContact { get; set; }
    }
}