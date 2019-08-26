using System.Linq;

using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SecondOuterContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

using ContactInformation = Mutators.Tests.FunctionalTests.InnerContract.ContactInformation;

namespace Mutators.Tests.FunctionalTests.ConverterCollections
{
    public static class SecondContractPartyInfoConfiguratorHelpers
    {
        public static void ConfigureParty<TSecondContract, TData, TSG2, TSG3, TSG5>(this ConverterConfigurator<TSecondContract, TSG2, TData, PartyInfo, PartyInfo> configurator)
            where TSG5 : IContactContainer
            where TSG2 : IReferenceArrayContainer<TSG3>, IContactInformationArrayContainer<TSG5>, INameAndAddressContainer, IFiiArrayContainer
            where TSG3 : IReferenceContainer
        {
            configurator.ConfigureIdentifiers<TSecondContract, TData, TSG2, TSG3, PartyInfo>();
            configurator.GoTo(info => info.Chief, sg2 => sg2.ContactInformationArray)
                        .ConfigureContact("MGR");
            configurator.GoTo(info => info, sg2 => sg2.FinancialInstitutionInformation)
                        .ConfigureBankInformation();
            configurator.ConfigureRussianPartyInfo<TSecondContract, TData, TSG2, TSG3, PartyInfo>();
            configurator.GoTo(info => info, sg2 => sg2.NameAndAddress)
                        .ConfigurePartyAddress();
        }

        public static void ConfigureIdentifiers<TSecondContract, TData, TSG2, TSG3, TPartyInfo>(
            this ConverterConfigurator<TSecondContract, TSG2, TData, TPartyInfo, TPartyInfo> configurator)
            where TSG2 : IReferenceArrayContainer<TSG3>, INameAndAddressContainer
            where TSG3 : IReferenceContainer
            where TPartyInfo : IContainsPartyIndentifiers
        {
            configurator.Target(party => party.Gln).Set(sg2 => sg2.NameAndAddress.PartyIdentificationDetails.PartyIdentifier);
            configurator.Target(party => party.SupplierCodeInBuyerSystem).Set(sg2 => (from sg3 in sg2.References
                                                                                      where sg3.Reference.ReferenceGroup.ReferenceCodeQualifier == "YC1"
                                                                                      select sg3.Reference.ReferenceGroup.ReferenceIdentifier).First());
        }

        public static void ConfigureBankInformation<TSecondContract, TData>(this ConverterConfigurator<TSecondContract, FinancialInstitutionInformation[], TData, PartyInfo, PartyInfo> configurator)
        {
            var fiiConfigurator = configurator.GoTo(party => party, fiis => fiis.First(information => information.PartyFunctionCodeQualifier == "BK"));
            fiiConfigurator.Target(party => party.BankAccount.BankAccountNumber).Set(information => information.HolderIdentification.HolderIdentification);
        }

        public static void ConfigureRussianPartyInfo<TSecondContract, TData, TSG2, TSG3, TPartyInfo>(
            this ConverterConfigurator<TSecondContract, TSG2, TData, TPartyInfo, TPartyInfo> configurator)
            where TSG2 : IReferenceArrayContainer<TSG3>, INameAndAddressContainer
            where TSG3 : IReferenceContainer
            where TPartyInfo : IContainsRussianPartyInfo
        {
            configurator.ConfigurePartyName();

            configurator.Target(party => party.RussianPartyInfo.RussianPartyType)
                        .Set(sg2 => new
                            {
                                sg2.NameAndAddress.PartyNameType.PartyNameFormatCode,
                                sg2.References.First(sg3 => sg3.Reference.ReferenceGroup.ReferenceCodeQualifier == "FC").Reference.ReferenceGroup.ReferenceIdentifier
                            }, o => GetRussianPartyType(o.PartyNameFormatCode, o.ReferenceIdentifier));
            configurator.Target(party => party.RussianPartyInfo.IPInfo.Inn)
                        .Set(sg2 => sg2.References.First(sg3 => sg3.Reference.ReferenceGroup.ReferenceCodeQualifier == "FC").Reference.ReferenceGroup.ReferenceIdentifier)
                        .NullifyIf(party => party.RussianPartyInfo.RussianPartyType != RussianPartyType.IP);
            configurator.Target(party => party.RussianPartyInfo.ULInfo.Inn)
                        .Set(sg2 => sg2.References.First(sg3 => sg3.Reference.ReferenceGroup.ReferenceCodeQualifier == "FC").Reference.ReferenceGroup.ReferenceIdentifier)
                        .NullifyIf(party => party.RussianPartyInfo.RussianPartyType != RussianPartyType.UL);
        }

        public static void ConfigurePartyAddress<TSecondContract, TData>(
            this ConverterConfigurator<TSecondContract, NameAndAddress, TData, PartyInfo, PartyInfo> configurator)
        {
            configurator.Target(party => party.PartyAddress.AddressType)
                        .Set(nameAndAddress => defaultConverter.Convert(nameAndAddress.CountryNameCode), code => GetAddressType(code));
            configurator.Target(party => party.PartyAddress.ForeignAddressInfo.CountryCode)
                        .Set(nameAndAddress => nameAndAddress.CountryNameCode,
                             s => defaultConverter.Convert(s),
                             s => s != null && defaultConverter.Convert(s) == null,
                             s => new TestText {Text = s})
                        .NullifyIf(party => party.PartyAddress.AddressType != AddressType.Foreign);

            configurator.Target(party => party.PartyAddress.ForeignAddressInfo.Address)
                        .Set(nameAndAddress => nameAndAddress.Street.StreetAndNumberOrPostBoxIdentifier, strings => ArrayStringConverter.ToString(strings))
                        .NullifyIf(party => party.PartyAddress.AddressType != AddressType.Foreign);

            configurator.GoTo(x => x.PartyAddress).ConfigureRussianPartyAddress();
            configurator.Target(party => party.PartyAddress.RussianAddressInfo).NullifyIf(party => party.PartyAddress.AddressType != AddressType.Russian);
        }

        public static AddressType GetAddressType(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode) || countryCode == "643")
                return AddressType.Russian;
            return AddressType.Foreign;
        }

        public static void ConfigurePartyName<TSecondContract, TData, TSG2, TPartyInfo>(
            this ConverterConfigurator<TSecondContract, TSG2, TData, TPartyInfo, TPartyInfo> configurator)
            where TSG2 : INameAndAddressContainer
            where TPartyInfo : IContainsRussianPartyInfo
        {
            configurator.Target(party => party.RussianPartyInfo.IPInfo.LastName)
                        .Set(sg2 => sg2.NameAndAddress.PartyNameType.PartyName[0])
                        .NullifyIf(party => party.RussianPartyInfo.RussianPartyType != RussianPartyType.IP);
            configurator.Target(party => party.RussianPartyInfo.IPInfo.FirstName)
                        .Set(sg2 => sg2.NameAndAddress.PartyNameType.PartyName[1])
                        .NullifyIf(party => party.RussianPartyInfo.RussianPartyType != RussianPartyType.IP);

            configurator.If(sg2 => sg2.NameAndAddress.PartyNameType.PartyName != null)
                        .Target(party => party.RussianPartyInfo.ULInfo.Name)
                        .Set(sg2 => sg2.NameAndAddress.PartyNameType.PartyName, strings => ArrayStringConverter.ToString(strings))
                        .NullifyIf(party => party.RussianPartyInfo.RussianPartyType != RussianPartyType.UL);

            configurator.If(sg2 => sg2.NameAndAddress.PartyNameType.PartyName == null)
                        .Target(party => party.RussianPartyInfo.ULInfo.Name)
                        .Set(sg2 => sg2.NameAndAddress.NameAndAddressGroup.NameAndAddressDescription, strings => ArrayStringConverter.ToString(strings))
                        .NullifyIf(party => party.RussianPartyInfo.RussianPartyType != RussianPartyType.UL);
        }

        public static RussianPartyType GetRussianPartyType(string partyNameFormatCode, string inn)
        {
            switch (partyNameFormatCode)
            {
            case "IN":
                return RussianPartyType.IP;
            case "LE":
                return RussianPartyType.UL;
            default:
                {
                    var length = (inn ?? "").Length;
                    switch (length)
                    {
                    case 12:
                        return RussianPartyType.IP;
                    case 10:
                        return RussianPartyType.UL;
                    default:
                        return RussianPartyType.UL;
                    }
                }
            }
        }

        public static void ConfigureRussianPartyAddress<TSecondContract, TData, TPartyInfo>(this ConverterConfigurator<TSecondContract, NameAndAddress, TData, TPartyInfo, TPartyInfo> configurator)
            where TPartyInfo : IContainsRussianPartyAdress, IContainsAddressType
        {
            configurator.Target(party => party.RussianAddressInfo.City)
                        .Set(nameAndAddress => nameAndAddress.CityName)
                        .NullifyIf(party => party.AddressType != AddressType.Russian);
            configurator.Target(party => party.RussianAddressInfo.Street)
                        .Set(nameAndAddress => nameAndAddress.Street.StreetAndNumberOrPostBoxIdentifier, strings => ArrayStringConverter.ToString(strings))
                        .NullifyIf(party => party.AddressType != AddressType.Russian);
        }

        private static void ConfigureContact<TContract, TData, TSG5>(
            this ConverterConfigurator<TContract, TSG5[], TData, ContactInformation, ContactInformation> configurator,
            string contactFunctionCode
            )
            where TSG5 : IContactContainer
        {
            var sg5Configurator = configurator.GoTo(data => data, sg5 => sg5.FirstOrDefault(cta => cta.ContactInformation.ContactFunctionCode == contactFunctionCode));
            sg5Configurator.Target(data => data.Name).Set(sgs5 => sgs5.ContactInformation.DepartmentOrEmployeeDetails.DepartmentOrEmployeeName);
            var contactConfigurator = sg5Configurator.GoTo(data => data, sgs5 => sgs5.CommunicationContact);

            contactConfigurator.Target(data => data.Phone).Set(contacts => contacts.FirstOrDefault(contact => contact.CommunicationContactGroup.CommunicationAddressCodeQualifier == "TE").CommunicationContactGroup.CommunicationAddressIdentifier);
        }

        private static readonly DefaultConverter defaultConverter = new DefaultConverter();
    }
}