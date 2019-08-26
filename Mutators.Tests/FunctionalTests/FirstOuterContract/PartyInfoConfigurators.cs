using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.FirstOuterContract
{
    public static class PartyInfoConfigurators
    {
        public static void ConfigureFromFirstContractToInner<TFirst, TInner>(
            ConverterConfigurator<TFirst, FirstContractPartyInfo, TInner, PartyInfo, PartyInfo> configurator, DefaultConverter defaultConverter)
        {
            configurator.Target(x => x.Gln).Set(x => x.Gln);

            configurator.Target(x => x.PartyInfoType).If(x => x.SelfEmployed != null || x.Organization != null).Set(x => PartyInfoType.Russian);
            configurator.Target(x => x.PartyInfoType).If(x => x.ForeignOrganization != null).Set(x => PartyInfoType.Foreign);
            configurator.Target(x => x.PartyAddress.AddressType).Set(x => x.ForeignAddress.IsEmpty() ? AddressType.Russian : AddressType.Foreign);
            configurator.Target(x => x.UsesSimplifiedTaxSystem).Set(x => x.TaxSystem == "Simplified");


            var russianConfigurator = configurator.If((x, y) => y.PartyAddress.AddressType == AddressType.Russian)
                                                  .GoTo(x => x.PartyAddress.RussianAddressInfo);
            russianConfigurator.Target(x => x.PostalCode).Set(x => x.RussianAddress.PostalCode);
            russianConfigurator.Target(x => x.City).Set(x => x.RussianAddress.City);
            
            var foreignConfigurator = configurator.If((x, y) => y.PartyAddress.AddressType == AddressType.Foreign)
                                                  .GoTo(x => x.PartyAddress.ForeignAddressInfo);
            foreignConfigurator.Target(x => x.CountryCode).Set(x => defaultConverter.Convert(x.ForeignAddress.CountryIsoCode));
            foreignConfigurator.Target(x => x.Address).Set(x => x.ForeignAddress.Address);

            configurator.Target(x => x.RussianPartyInfo.RussianPartyType).Set(x => x.SelfEmployed == null ? RussianPartyType.UL : RussianPartyType.IP);
            
            var ulConfigurator = configurator.If((x, y) => y.RussianPartyInfo.RussianPartyType == RussianPartyType.UL)
                                             .GoTo(x => x.RussianPartyInfo.ULInfo);
            ulConfigurator.Target(x => x.Inn).Set(x => x.Organization.Inn);
            ulConfigurator.Target(x => x.Name).Set(x => x.Organization.Name);

            var ipConfigurator = configurator.If((x, y) => y.RussianPartyInfo.RussianPartyType == RussianPartyType.IP).
                                              GoTo(x => x.RussianPartyInfo.IPInfo);
            ipConfigurator.Target(x => x.Inn).Set(x => x.SelfEmployed.Inn);
            ipConfigurator.Target(x => x.FirstName).Set(x => x.SelfEmployed.FullName.FirstName);
            ipConfigurator.Target(x => x.LastName).Set(x => x.SelfEmployed.FullName.LastName);

            var chiefConfigurator = configurator.GoTo(x => x.Chief, x => x.ContactInfo.Ceo);
            chiefConfigurator.Target(x => x.Name).Set(x => x.Name);
            chiefConfigurator.Target(x => x.Phone).Set(x => x.Phone);

            configurator.If(x => string.IsNullOrEmpty(x.ContactInfo.Ceo.Phone)).Target(x => x.Chief.Phone).Set(x => x.AdditionalInfo.Phone);
            configurator.If(x => string.IsNullOrEmpty(x.ContactInfo.Ceo.Name)).Target(x => x.Chief.Name).Set(x => x.AdditionalInfo.NameOfCeo);
        }

        public static void ConfigureFromInnerToFirstContract(ConverterConfigurator<InnerDocument, PartyInfo, FirstContractDocument, FirstContractPartyInfo, FirstContractPartyInfo> configurator, DefaultConverter defaultConverter)
        {
            configurator.Target(x => x.Gln).Set(x => x.Gln);
            configurator.Target(x => x.TaxSystem).If(x => x.UsesSimplifiedTaxSystem).Set("Simplified");

            var foreignConfigurator = configurator.If(x => x.PartyInfoType == PartyInfoType.Foreign)
                                                  .GoTo(x => x.ForeignOrganization, x => x.ForeignPartyInfo);
            foreignConfigurator.Target(x => x.Name).Set(x => x.Name);

            var orgConfigurator = configurator.GoTo(x => x.Organization, x => x.RussianPartyInfo.ULInfo);
            orgConfigurator.Target(x => x.Inn).Set(x => x.Inn);
            orgConfigurator.Target(x => x.Name).Set(x => x.Name);

            var ipConfigurator = configurator.GoTo(x => x.SelfEmployed, x => x.RussianPartyInfo.IPInfo);
            ipConfigurator.Target(x => x.Inn).Set(x => x.Inn);
            ipConfigurator.Target(x => x.FullName.FirstName).Set(x => x.FirstName);
            ipConfigurator.Target(x => x.FullName.LastName).Set(x => x.LastName);

            var rusAddrConfigurator = configurator.GoTo(x => x.RussianAddress, x => x.PartyAddress.RussianAddressInfo);
            rusAddrConfigurator.Target(x => x.PostalCode).Set(x => x.PostalCode);
            rusAddrConfigurator.Target(x => x.City).Set(x => x.City);

            var foreignAddrConfigurator = configurator.GoTo(x => x.ForeignAddress, x => x.PartyAddress.ForeignAddressInfo);
            foreignAddrConfigurator.Target(x => x.Address).Set(x => x.Address);
            foreignAddrConfigurator.Target(x => x.CountryIsoCode).Set(x => defaultConverter.Convert(x.CountryCode));

            var chiefConfigurator = configurator.GoTo(x => x.ContactInfo.Ceo, x => x.Chief);
            chiefConfigurator.Target(x => x.Name).Set(x => x.Name);
            chiefConfigurator.Target(x => x.Phone).Set(x => x.Phone);
            
            var aiConfigurator = configurator.GoTo(x => x.AdditionalInfo);
            aiConfigurator.Target(x => x.Phone).Set(x => string.IsNullOrEmpty(x.OrderContact.Phone) ? x.Chief.Phone : x.OrderContact.Phone);
            aiConfigurator.Target(x => x.NameOfCeo).Set(x => string.IsNullOrEmpty(x.OrderContact.Name) ? x.Chief.Name : x.OrderContact.Name);
        }
    }
}