using System;

using FluentAssertions;

using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.ConverterCollections;
using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SecondOuterContract;

using NUnit.Framework;

using ContactInformation = Mutators.Tests.FunctionalTests.SecondOuterContract.ContactInformation;
using Package = Mutators.Tests.FunctionalTests.SecondOuterContract.Package;
using Quantity = Mutators.Tests.FunctionalTests.SecondOuterContract.Quantity;

namespace Mutators.Tests.FunctionalTests.ConverterTests
{
    [TestFixture]
    public class SecondContractToInnerTest
    {
        [Test]
        public void TestNoneContext()
        {
            #region second contract

            var secondContractDocument = new SecondContractDocument<SecondContractDocumentBody>
                {
                    SG0 = new[]
                        {
                            new SecondContractDocumentBody
                                {
                                    BeginningOfMessage = new BeginningOfMessage
                                        {
                                            DocumentMessageIdentification = new DocumentMessageIdentification
                                                {
                                                    DocumentIdentifier = "Message123",
                                                },
                                        },
                                    ControlTotal = new[]
                                        {
                                            new ControlTotal
                                                {
                                                    Control = new Control
                                                        {
                                                            ControlTotalTypeCodeQualifier = "1",
                                                            ControlTotalValue = "10.1",
                                                        },
                                                },
                                            new ControlTotal
                                                {
                                                    Control = new Control
                                                        {
                                                            ControlTotalTypeCodeQualifier = "11",
                                                            ControlTotalValue = "12.34",
                                                        },
                                                },
                                        },
                                    Currency = new Currencies
                                        {
                                            CurrencyDetails = new[]
                                                {
                                                    new CurrencyDetails
                                                        {
                                                            UsageCodeQualifier = "1",
                                                            TypeCodeQualifier = "2",
                                                            IdentificationCode = "first",
                                                        },
                                                    new CurrencyDetails
                                                        {
                                                            UsageCodeQualifier = "2",
                                                            TypeCodeQualifier = "4",
                                                            IdentificationCode = "second",
                                                        },
                                                    new CurrencyDetails
                                                        {
                                                            UsageCodeQualifier = "2",
                                                            TypeCodeQualifier = "4",
                                                            IdentificationCode = "third",
                                                        },
                                                }
                                        },
                                    FreeText = new[]
                                        {
                                            new FreeText
                                                {
                                                    TextSubjectCodeQualifier = "PUR",
                                                    TextLiteral = new TextLiteral
                                                        {
                                                            FreeTextValue = new[] {"purchasing", " info"},
                                                        },
                                                }
                                        },
                                    References = new[]
                                        {
                                            new SG1
                                                {
                                                    Reference = new Reference
                                                        {
                                                            ReferenceGroup = new ReferenceGroup
                                                                {
                                                                    ReferenceIdentifier = "blanket number",
                                                                    ReferenceCodeQualifier = "BO",
                                                                },
                                                        },
                                                },
                                        },
                                    SG10 = new[]
                                        {
                                            new SG10
                                                {
                                                    DetailsOfTransport = new DetailsOfTransport
                                                        {
                                                            TransportStageCodeQualifier = "1",
                                                            TransportIdentification = new TransportIdentification
                                                                {
                                                                    TransportMeansIdentificationName = "vehicle number",
                                                                },
                                                        },
                                                },
                                            new SG10
                                                {
                                                    DetailsOfTransport = new DetailsOfTransport
                                                        {
                                                            TransportStageCodeQualifier = "20",
                                                            TransportMeans = new TransportMeans
                                                                {
                                                                    TransportMeansDescription = "transport means",
                                                                    TransportMeansDescriptionCode = "transport means code",
                                                                },
                                                        },
                                                    SG11 = new[]
                                                        {
                                                            new SG11
                                                                {
                                                                    DateTimePeriod = new[]
                                                                        {
                                                                            new DateTimePeriod
                                                                                {
                                                                                    DateTimePeriodGroup = new DateTimePeriodGroup
                                                                                        {
                                                                                            FunctionCodeQualifier = "232",
                                                                                            FormatCode = "102",
                                                                                            Value = "20130314",
                                                                                        }
                                                                                },
                                                                        },
                                                                },
                                                        },
                                                },
                                        },
                                    SG28 = new[]
                                        {
                                            new SG28
                                                {
                                                    FreeText = new[]
                                                        {
                                                            new FreeText
                                                                {
                                                                    TextSubjectCodeQualifier = "DEL",
                                                                    TextReference = new TextReference
                                                                        {
                                                                            CodeListResponsibleAgencyCode = "ZZZ",
                                                                            FreeTextValueCode = "flow type",
                                                                        },
                                                                },
                                                        },
                                                    ItemDescription = new[]
                                                        {
                                                            new ItemDescription
                                                                {
                                                                    DescriptionFormatCode = "C",
                                                                    ItemDescriptionGroup = new ItemDescriptionGroup
                                                                        {
                                                                            ItemDescriptionCode = "RC",
                                                                            ItemDescription = new[] {"first ", "good item", " name"}
                                                                        },
                                                                },
                                                        },
                                                    LineItem = new LineItem
                                                        {
                                                            ItemNumberIdentification = new ItemNumberIdentification {ItemIdentifier = "GTIN 1"},
                                                        },
                                                    AdditionalProductId = new[]
                                                        {
                                                            new AdditionalProductId
                                                                {
                                                                    ItemNumberIdentification = new[]
                                                                        {
                                                                            new ItemNumberIdentification
                                                                                {
                                                                                    ItemIdentifier = "additional id1",
                                                                                    ItemTypeIdentificationCode = "STB",
                                                                                },
                                                                        },
                                                                    ProductIdentifierCodeQualifier = "5",
                                                                },
                                                        },
                                                    MonetaryAmount = new[]
                                                        {
                                                            new MonetaryAmount
                                                                {
                                                                    MonetaryAmountGroup = new MonetaryAmountGroup
                                                                        {
                                                                            MonetaryAmountTypeCodeQualifier = "161",
                                                                            MonetaryAmount = "22.33",
                                                                        },
                                                                },
                                                        },
                                                    AdditionalInformation = new[]
                                                        {
                                                            new AdditionalInformation
                                                                {
                                                                    CountryOfOriginNameCode = "DefaultCountry",
                                                                },
                                                            new AdditionalInformation
                                                                {
                                                                    CountryOfOriginNameCode = "country",
                                                                },
                                                        },
                                                    SG34 = new[]
                                                        {
                                                            new SG34
                                                                {
                                                                    Package = new Package
                                                                        {
                                                                            PackageType = new PackageType
                                                                                {
                                                                                    PackageTypeDescriptionCode = "default",
                                                                                },
                                                                            PackageQuantity = "321.3",
                                                                        },
                                                                    Quantity = new[]
                                                                        {
                                                                            new Quantity
                                                                                {
                                                                                    QuantityDetails = new QuantityDetails
                                                                                        {
                                                                                            QuantityTypeCodeQualifier = "52",
                                                                                            Quantity = "0.01",
                                                                                            MeasurementUnitCode = "DEFAULT",
                                                                                        },
                                                                                },
                                                                        },
                                                                },
                                                            new SG34
                                                                {
                                                                    Package = new Package
                                                                        {
                                                                            PackageType = new PackageType
                                                                                {
                                                                                    PackageTypeDescriptionCode = "package type code",
                                                                                },
                                                                            PackageQuantity = "324.3",
                                                                        },
                                                                    Quantity = new[]
                                                                        {
                                                                            new Quantity
                                                                                {
                                                                                    QuantityDetails = new QuantityDetails
                                                                                        {
                                                                                            QuantityTypeCodeQualifier = "52",
                                                                                            Quantity = "0.02",
                                                                                            MeasurementUnitCode = "PCE",
                                                                                        },
                                                                                },
                                                                        },
                                                                }
                                                        },
                                                },
                                        },
                                    MonetaryAmount = new[]
                                        {
                                            new MonetaryAmount
                                                {
                                                    MonetaryAmountGroup = new MonetaryAmountGroup
                                                        {
                                                            MonetaryAmountTypeCodeQualifier = "79",
                                                            MonetaryAmount = "34.21",
                                                        },
                                                },
                                            new MonetaryAmount
                                                {
                                                    MonetaryAmountGroup = new MonetaryAmountGroup
                                                        {
                                                            MonetaryAmountTypeCodeQualifier = "9",
                                                            MonetaryAmount = "3.14",
                                                        },
                                                },
                                        },
                                    PartiesArray = new[]
                                        {
                                            new SG2
                                                {
                                                    NameAndAddress = new NameAndAddress
                                                        {
                                                            PartyFunctionCodeQualifier = "PW",
                                                            PartyIdentificationDetails = new PartyIdentificationDetails
                                                                {
                                                                    PartyIdentifier = "GLN",
                                                                },
                                                            PartyNameType = new PartyNameType
                                                                {
                                                                    PartyName = new[] {"last name", "first name"},
                                                                    PartyNameFormatCode = "IN",
                                                                },
                                                            CountryNameCode = "foreign country code",
                                                            Street = new Street
                                                                {
                                                                    StreetAndNumberOrPostBoxIdentifier = new[] {"street ", "number ", "postbox"}
                                                                },
                                                            PostalIdentificationCode = "post code",
                                                        },
                                                    References = new[]
                                                        {
                                                            new SG3
                                                                {
                                                                    Reference = new Reference
                                                                        {
                                                                            ReferenceGroup = new ReferenceGroup
                                                                                {
                                                                                    ReferenceCodeQualifier = "YC1",
                                                                                    ReferenceIdentifier = "Supplier Code",
                                                                                },
                                                                        },
                                                                },
                                                            new SG3
                                                                {
                                                                    Reference = new Reference
                                                                        {
                                                                            ReferenceGroup = new ReferenceGroup
                                                                                {
                                                                                    ReferenceCodeQualifier = "FC",
                                                                                    ReferenceIdentifier = "inn ip",
                                                                                },
                                                                        },
                                                                },
                                                        },
                                                    ContactInformationArray = new[]
                                                        {
                                                            new SG5
                                                                {
                                                                    ContactInformation = new ContactInformation
                                                                        {
                                                                            ContactFunctionCode = "MGR",
                                                                            DepartmentOrEmployeeDetails = new DepartmentOrEmployeeDetails
                                                                                {
                                                                                    DepartmentOrEmployeeName = "chief name",
                                                                                },
                                                                        },
                                                                    CommunicationContact = new[]
                                                                        {
                                                                            new CommunicationContact
                                                                                {
                                                                                    CommunicationContactGroup = new CommunicationContactGroup
                                                                                        {
                                                                                            CommunicationAddressCodeQualifier = "TE",
                                                                                            CommunicationAddressIdentifier = "phone",
                                                                                        },
                                                                                },
                                                                        },
                                                                },
                                                        },
                                                    FinancialInstitutionInformation = new[]
                                                        {
                                                            new FinancialInstitutionInformation
                                                                {
                                                                    PartyFunctionCodeQualifier = "BK",
                                                                    HolderIdentification = new HolderIdentificationType
                                                                        {
                                                                            HolderIdentification = "bank account number",
                                                                        },
                                                                },
                                                        },
                                                },
                                            new SG2
                                                {
                                                    NameAndAddress = new NameAndAddress
                                                        {
                                                            PartyFunctionCodeQualifier = "PW",
                                                            PartyIdentificationDetails = new PartyIdentificationDetails
                                                                {
                                                                    PartyIdentifier = "GLN1",
                                                                },
                                                            PartyNameType = new PartyNameType
                                                                {
                                                                    PartyName = new[] {"ul ", "name"},
                                                                    PartyNameFormatCode = "LE",
                                                                },
                                                            CountryNameCode = null,
                                                            Street = new Street
                                                                {
                                                                    StreetAndNumberOrPostBoxIdentifier = new[] {"str", "eet"},
                                                                },
                                                            PostalIdentificationCode = "post code",
                                                            CityName = "city",
                                                        },
                                                    References = new[]
                                                        {
                                                            new SG3
                                                                {
                                                                    Reference = new Reference
                                                                        {
                                                                            ReferenceGroup = new ReferenceGroup
                                                                                {
                                                                                    ReferenceCodeQualifier = "FC",
                                                                                    ReferenceIdentifier = "inn ul",
                                                                                },
                                                                        },
                                                                },
                                                        },
                                                }
                                        },
                                },
                        },
                };

            #endregion

            #region inner document

            var innerDocument = new InnerDocument
                {
                    OrdersNumber = "Message123",
                    CurrencyCode = "second",
                    FlowType = "flow type",
                    TransportDetails = new TransportDetails
                        {
                            VehicleNumber = "vehicle number",
                        },
                    FreeText = "purchasing info",
                    Transports = new[]
                        {
                            new TransportDetails
                                {
                                    TypeOfTransport = "transport means",
                                    TypeOfTransportCode = "transport means code",
                                    DeliveryDateForVehicle = new DateTime(2013, 03, 14, 0, 0, 0, DateTimeKind.Utc),
                                },
                        },
                    DespatchParties = new[]
                        {
                            new DespatchPartyInfo
                                {
                                    PartyInfo = new PartyInfo
                                        {
                                            Gln = "GLN",
                                            SupplierCodeInBuyerSystem = "Supplier Code",
                                            PartyAddress = new PartyAddress
                                                {
                                                    AddressType = AddressType.Foreign,
                                                    RussianAddressInfo = new RussianAddressInfo(),
                                                    ForeignAddressInfo = new ForeignAddressInfo
                                                        {
                                                            CountryCode = "foreign country code",
                                                            Address = "street number postbox",
                                                        },
                                                },
                                            RussianPartyInfo = new RussianPartyInfo
                                                {
                                                    RussianPartyType = RussianPartyType.IP,
                                                    IPInfo = new IpInfo
                                                        {
                                                            LastName = "last name",
                                                            FirstName = "first name",
                                                            Inn = "inn ip",
                                                        },
                                                    ULInfo = new UlInfo(),
                                                },
                                            BankAccount = new BankAccount
                                                {
                                                    BankAccountNumber = "bank account number",
                                                },
                                            Chief = new InnerContract.ContactInformation
                                                {
                                                    Name = "chief name",
                                                    Phone = "phone",
                                                },
                                        },
                                },
                            new DespatchPartyInfo
                                {
                                    PartyInfo = new PartyInfo
                                        {
                                            Gln = "GLN1",
                                            PartyAddress = new PartyAddress
                                                {
                                                    AddressType = AddressType.Russian,
                                                    RussianAddressInfo = new RussianAddressInfo
                                                        {
                                                            Street = "street",
                                                            City = "city",
                                                        },
                                                    ForeignAddressInfo = new ForeignAddressInfo(),
                                                },
                                            RussianPartyInfo = new RussianPartyInfo
                                                {
                                                    RussianPartyType = RussianPartyType.UL,
                                                    IPInfo = new IpInfo(),
                                                    ULInfo = new UlInfo
                                                        {
                                                            Inn = "inn ul",
                                                            Name = "ul name",
                                                        },
                                                },
                                            BankAccount = new BankAccount(),
                                            Chief = new InnerContract.ContactInformation(),
                                        },
                                }
                        },
                    BlanketOrdersNumber = "blanket number",
                    OrdersTotalPackageQuantity = 12.34m,
                    RecadvTotal = 34.21m,
                    TotalWithVAT = 3.14m,
                    GoodItems = new[]
                        {
                            new CommonGoodItem
                                {
                                    GoodNumber = new GoodNumber {Number = 1},
                                    Name = "first good item name",
                                    GTIN = "GTIN 1",
                                    AdditionalId = "additional id1",
                                    CountriesOfOriginCode = new[] {null, "country"},
                                    IsReturnableContainer = true,
                                    TypeOfUnit = "RC",
                                    ExciseTax = 22.33m,
                                    Packages = new[]
                                        {
                                            new PackageForItem
                                                {
                                                    PackageTypeCode = null,
                                                    OnePackageQuantity = new InnerContract.Quantity
                                                        {
                                                            Value = 0.01m,
                                                            MeasurementUnitCode = "DME",
                                                        },
                                                    Quantity = 321.3m,
                                                },
                                            new PackageForItem
                                                {
                                                    PackageTypeCode = "package type code",
                                                    OnePackageQuantity = new InnerContract.Quantity
                                                        {
                                                            Value = 0.02m,
                                                            MeasurementUnitCode = "PCE",
                                                        },
                                                    Quantity = 324.3m,
                                                },
                                        },
                                },
                        },
                };

            #endregion

            var converter = converterCollection.GetConverter(new TestConverterContext());
            converter(secondContractDocument).Should().BeEquivalentTo(innerDocument, config => config.Excluding(x => x.OrdersDate));
        }

        private readonly SecondContractToInnerConverterCollection converterCollection = new SecondContractToInnerConverterCollection(
            new PathFormatterCollection(),
            new TestStringConverter());
    }
}