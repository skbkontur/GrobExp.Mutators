using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.SecondOuterContract
{
    public static class SecondContractConvertingHelpers
    {
        public static void ConfigureIdentifiers<TData, TMessage>(
            ConverterConfigurator<TData, PartyInfo, TMessage, NameAndAddress, NameAndAddress> configurator,
            string functionCodeQualifier)
        {
            ConfigureIdentifiers(configurator, x => functionCodeQualifier);
        }

        public static void ConfigureIdentifiers<TData, TMessage>(
            ConverterConfigurator<TData, PartyInfo, TMessage, NameAndAddress, NameAndAddress> configurator,
            Expression<Func<PartyInfo, string>> getFunctionCodeQualifier)
        {
            configurator.Target(nameAndAddress => nameAndAddress.PartyFunctionCodeQualifier).Set(getFunctionCodeQualifier);
            configurator.Target(nameAndAddress => nameAndAddress.PartyIdentificationDetails.PartyIdentifier).Set(party => party.Gln);
        }

        public static void ConfigureReferences<TData, TMessage, TSg>(ConverterConfigurator<TData, PartyInfo, SecondContractDocument<TMessage>, TSg[], TSg[]> configurator)
            where TSg : IReferenceContainer
        {
            var ulConfigurator = configurator.If(party => party.RussianPartyInfo.RussianPartyType == RussianPartyType.UL).GoTo(sgs => sgs, party => party.RussianPartyInfo.ULInfo);
            ulConfigurator.GoTo(sgs => sgs[0]).SetReference("FC", party => party.Inn);
            ulConfigurator.GoTo(sgs => sgs[1]).SetReference("GN", party => party.OKPOCode);

            var ipConfigurator = configurator.If(party => party.RussianPartyInfo.RussianPartyType == RussianPartyType.IP).GoTo(sgs => sgs, party => party.RussianPartyInfo.IPInfo);
            ipConfigurator.GoTo(sgs => sgs[0]).SetReference("FC", party => party.Inn);
            ipConfigurator.GoTo(sgs => sgs[1]).SetReference("GN", party => party.OKPOCode);
        }

        public static void ConfigureReference<TSecondContract, TData, T, TSg>(this ConverterConfigurator<TSecondContract, TSg[], TData, T, T> configurator,
                                                                              string referenceCodeQualifier, Expression<Func<T, string>> pathToNumber)
            where TSg : IReferenceContainer
        {
            var sgConfigurator = configurator.GoTo(data => data, references => references.First(sg => sg.Reference.ReferenceGroup.ReferenceCodeQualifier == referenceCodeQualifier));
            sgConfigurator.Target(pathToNumber).Set(sg => sg.Reference.ReferenceGroup.ReferenceIdentifier);
        }

        public static void ConfigureNameAndAddress<TInner, TSecondContract>(ConverterConfigurator<TInner, PartyInfo, TSecondContract, NameAndAddress, NameAndAddress> configurator, TestConverterContext context, DefaultConverter defaultConverter)
        {
            configurator.Target(nameAndAddress => nameAndAddress.PartyNameType.PartyName)
                        .Set(party => party.RussianPartyInfo.RussianPartyType == RussianPartyType.IP
                                          ? new[] {party.RussianPartyInfo.IPInfo.LastName, party.RussianPartyInfo.IPInfo.FirstName, party.RussianPartyInfo.IPInfo.MiddleName}
                                          : ArrayStringConverter.ToArrayString(party.RussianPartyInfo.ULInfo.Name, 35, 5));

            configurator.Target(nameAndAddress => nameAndAddress.PartyNameType.PartyNameFormatCode)
                        .Set(party => party.RussianPartyInfo.RussianPartyType == RussianPartyType.IP ? "IP" : "UL");

            var addressConfigurator = configurator.GoTo(address => address, party => party.PartyAddress);
            addressConfigurator.If((x, address) => address.CityName != null
                                                   || address.CountrySubEntityDetails.CountrySubEntityNameCode != null
                                                   || address.CountrySubEntityDetails.CountrySubEntityName != null
                                                   || address.PostalIdentificationCode != null
                                                   || address.Street.StreetAndNumberOrPostBoxIdentifier != null)
                               .Target(address => address.CountryNameCode)
                               .Set(address => defaultConverter.Convert(address.AddressType == AddressType.Russian ? "643" : address.ForeignAddressInfo.CountryCode));

            var useSemicolon = new[] {MutatorsContextType.None}.Contains(context.MutatorsContextType);
            addressConfigurator.Target(address => address.Street.StreetAndNumberOrPostBoxIdentifier)
                               .Set(address => address.AddressType == AddressType.Russian
                                                   ? new[] {address.RussianAddressInfo.Street, address.RussianAddressInfo.House, address.RussianAddressInfo.Flat}.JoinIgnoreEmpty(useSemicolon ? "; " : ", ")
                                                   : address.ForeignAddressInfo.Address,
                                    s => ArrayStringConverter.ToArrayString(s, 35, 4));

            addressConfigurator.Target(address => address.CityName)
                               .Set(address => address.AddressType == AddressType.Russian
                                                   ? new[] {address.RussianAddressInfo.City, address.RussianAddressInfo.Village}.JoinIgnoreEmpty(", ") : null);

            addressConfigurator.Target(address => address.CountrySubEntityDetails.CountrySubEntityName)
                               .Set(address => address.AddressType == AddressType.Russian
                                                   ? new[] {defaultConverter.Convert(address.RussianAddressInfo.RegionCode), address.RussianAddressInfo.District}.JoinIgnoreEmpty(", ") : null);

            addressConfigurator.Target(address => address.PostalIdentificationCode)
                               .Set(address => address.AddressType == AddressType.Russian ? address.RussianAddressInfo.PostalCode : null);
        }

        public static void ConfigureFinancialInstitutionInformation<TData, TMessage>(
            ConverterConfigurator<TData, PartyInfo, TMessage, FinancialInstitutionInformation[], FinancialInstitutionInformation[]> configurator)
        {
            configurator.GoTo(informations => informations[0])
                        .BatchSet((information, party) => new Batch
                            {
                                {information.PartyFunctionCodeQualifier, "BK"},
                                {information.HolderIdentification.HolderIdentification.NotNull(), party.BankAccount.BankAccountNumber},
                                {information.HolderIdentification.HolderName[0], party.BankAccount.CorrespondentAccountNumber},
                                {information.InstitutionIdentification.InstitutionName.NotNull(), party.BankAccount.BankName},
                            });
        }

        public static IEnumerable<FreeText> FreeTexts(this CommonGoodItem item, DefaultConverter defaultConverter)
        {
            if (item == null)
                yield break;

            if (item.FlowType != null)
            {
                yield return new FreeText
                    {
                        TextSubjectCodeQualifier = "DEL",
                        FreeTextFunctionCode = "ZZZ",
                        TextLiteral = new TextLiteral {FreeTextValue = new[] {defaultConverter.Convert(item.FlowType)}},
                    };
            }

            var freeTextItems = new[]
                {
                    new {subjectCodeQualifier = "PRD", value = item.Dimensions},
                    new {subjectCodeQualifier = "ACB", value = item.Comment},
                };

            foreach (var freeTextItem in freeTextItems)
            {
                if (freeTextItem.value != null)
                    yield return new FreeText
                        {
                            TextSubjectCodeQualifier = freeTextItem.subjectCodeQualifier,
                            TextLiteral = new TextLiteral {FreeTextValue = ArrayStringConverter.ToArrayString(freeTextItem.value, 512, 5)},
                        };
            }
        }

        public static void ConfigureDate<TSecondContract, TData, T, TSg>(this ConverterConfigurator<TSecondContract, TSg, TData, T, T> configurator,
                                                                         string dateTimePeriodFunctionCodeQualifier,
                                                                         Expression<Func<T, DateTime?>> pathToDate)
            where TSg : IDtmArrayContainer
        {
            configurator.Target(pathToDate)
                        .Set(message => message.DateTimePeriod.FirstOrDefault(period => period.DateTimePeriodGroup.FunctionCodeQualifier == dateTimePeriodFunctionCodeQualifier),
                             period => StaticDateTimePeriodConverter.ToDateTime(period.DateTimePeriodGroup));
        }

        public static void ConfigureDateOrUtcNow<TSecondContract, TData, T, TSg>(this ConverterConfigurator<TSecondContract, TSg, TData, T, T> configurator,
                                                                                 string dateTimePeriodFunctionCodeQualifier,
                                                                                 Expression<Func<T, DateTime?>> pathToDate)
            where TSg : IDtmArrayContainer
        {
            configurator.Target(pathToDate)
                        .Set(message => message.DateTimePeriod.FirstOrDefault(period => period.DateTimePeriodGroup.FunctionCodeQualifier == dateTimePeriodFunctionCodeQualifier),
                             period => StaticDateTimePeriodConverter.ToDateTimeOrUtcNow(period.DateTimePeriodGroup));
        }

        public static IEnumerable<ReferenceInfo> GetReferencesArray(InnerDocument document)
        {
            yield return new ReferenceInfo(document.OrdersNumber, document.OrdersDate, "ON");

            foreach (var contract in document.Contracts?.Where(x => x != null) ?? new ContractInfo[0])
                yield return new ReferenceInfo(contract.ContractNumber, contract.ContractDate, "CT");

            yield return new ReferenceInfo(document.BlanketOrdersNumber, null, "BO");
        }

        public static void SetReferenceWithDatesArray<TData, TReferencePath, TMessage, TSg>(this ConverterConfigurator<TData, TReferencePath, SecondContractDocument<TMessage>, TSg, TSg> configurator,
                                                                                            Expression<Func<TReferencePath, string>> pathToCodeQualifier,
                                                                                            Expression<Func<TReferencePath, string>> pathToNumber,
                                                                                            Expression<Func<TReferencePath, DateTime?>> pathToDate,
                                                                                            string dateTimePeriodFormatCode = "203")
            where TSg : IReferenceContainer, IDtmArrayContainer
        {
            configurator.SetReference(pathToCodeQualifier, pathToNumber);
            configurator.Target(sg1 => sg1.DateTimePeriod[0]).Set(pathToDate, dateTime => StaticDateTimePeriodConverter.ToDateTimePeriod(dateTime, "171", dateTimePeriodFormatCode));
        }

        public static void SetReference<TData, TReferencePath, TMessage, TSg>(this ConverterConfigurator<TData, TReferencePath, SecondContractDocument<TMessage>, TSg, TSg> configurator,
                                                                              string codeQualifier,
                                                                              Expression<Func<TReferencePath, string>> pathToNumber)
            where TSg : IReferenceContainer
        {
            configurator.SetReference(x => codeQualifier, pathToNumber);
        }

        public static void SetReference<TData, TReferencePath, TMessage, TSg>(this ConverterConfigurator<TData, TReferencePath, SecondContractDocument<TMessage>, TSg, TSg> configurator,
                                                                              Expression<Func<TReferencePath, string>> pathToCodeQualifier,
                                                                              Expression<Func<TReferencePath, string>> pathToNumber)
            where TSg : IReferenceContainer
        {
            configurator.Target(sg1 => sg1.Reference.ReferenceGroup.ReferenceCodeQualifier).Set(pathToCodeQualifier);
            configurator.Target(sg1 => sg1.Reference.ReferenceGroup.ReferenceIdentifier).Set(pathToNumber);
        }
    }
}