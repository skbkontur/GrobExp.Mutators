namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public class SG2 : IReferenceArrayContainer<SG3>, IContactInformationArrayContainer<SG5>, INameAndAddressContainer, IFiiArrayContainer
    {
        public NameAndAddress NameAndAddress { get; set; }

        public FinancialInstitutionInformation[] FinancialInstitutionInformation { get; set; }

        public SG3[] References { get; set; }

        public SG5[] ContactInformationArray { get; set; }
    }
}