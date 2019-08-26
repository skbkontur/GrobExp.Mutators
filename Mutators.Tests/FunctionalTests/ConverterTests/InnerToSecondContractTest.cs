using System;

using FluentAssertions;

using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.ConverterCollections;
using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SecondOuterContract;

using NUnit.Framework;

using SecondContractQuantity = Mutators.Tests.FunctionalTests.SecondOuterContract.Quantity;
using InnerQuantity = Mutators.Tests.FunctionalTests.InnerContract.Quantity;
using Package = Mutators.Tests.FunctionalTests.SecondOuterContract.Package;

namespace Mutators.Tests.FunctionalTests.ConverterTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class InnerToSecondContractTest
    {
        [Test]
        public void TestNoneContext()
        {
            #region inner document

            var innerDocument = new InnerDocument
                {
                    OrdersNumber = "Orders123",
                    OrdersDate = new DateTime(2014, 11, 22, 0, 0, 0, DateTimeKind.Utc),
                    BlanketOrdersNumber = "bo123",
                    Contracts = new[]
                        {
                            new ContractInfo
                                {
                                    ContractNumber = "CN1",
                                    ContractDate = new DateTime(2014, 04, 04, 0, 0, 0, DateTimeKind.Utc),
                                },
                            null,
                            new ContractInfo
                                {
                                    ContractNumber = "CN2",
                                    ContractDate = new DateTime(2014, 05, 05, 0, 0, 0, DateTimeKind.Utc),
                                },
                        },
                    Supplier = new PartyInfo
                        {
                            Gln = "supplier gln",
                            RussianPartyInfo = new RussianPartyInfo
                                {
                                    RussianPartyType = RussianPartyType.UL,
                                    ULInfo = new UlInfo
                                        {
                                            Inn = "supplier inn",
                                            OKPOCode = "supplier okpo",
                                            Name = "Very long supplier name for ArrayStringConverterTest",
                                        },
                                },
                            PartyAddress = new PartyAddress
                                {
                                    AddressType = AddressType.Russian,
                                    RussianAddressInfo = new RussianAddressInfo
                                        {
                                            City = "City",
                                            Village = "Village",
                                            Street = "Street",
                                            House = "House",
                                            Flat = "Flat",
                                            RegionCode = "R1",
                                            District = "D1",
                                        }
                                },
                            BankAccount = new BankAccount
                                {
                                    BankAccountNumber = "bn1",
                                    CorrespondentAccountNumber = "cn1",
                                    BankName = "Bank",
                                },
                        },
                    Buyer = new PartyInfo
                        {
                            Gln = "buyer gln",
                            RussianPartyInfo = new RussianPartyInfo
                                {
                                    RussianPartyType = RussianPartyType.IP,
                                    IPInfo = new IpInfo
                                        {
                                            Inn = "buyer inn",
                                            OKPOCode = "buyer okpo",
                                            FirstName = "First",
                                            LastName = "Last",
                                            MiddleName = "Middle",
                                        }
                                },
                            PartyAddress = new PartyAddress
                                {
                                    AddressType = AddressType.Foreign,
                                    ForeignAddressInfo = new ForeignAddressInfo
                                        {
                                            CountryCode = "121",
                                            Address = "Some Foreign Address",
                                        }
                                },
                            BankAccount = new BankAccount
                                {
                                    BankAccountNumber = null,
                                    CorrespondentAccountNumber = "cn2",
                                    BankName = null,
                                },
                        },
                    GoodItems = new[]
                        {
                            new CommonGoodItem
                                {
                                    GTIN = "GTIN 1",
                                    BuyerProductId = "bid1",
                                    SupplierProductId = null,
                                    Quantity = new InnerQuantity
                                        {
                                            Value = 12.34m,
                                            MeasurementUnitCode = "PCE",
                                        },
                                    PriceSummary = 0.12m,
                                    SerialNumber = "SN1",
                                },
                            new CommonGoodItem
                                {
                                    GTIN = "GTIN 2",
                                    BuyerProductId = null,
                                    SupplierProductId = "sid2",
                                    FlowType = "Flow type",
                                    Comment = "Comment",
                                    Quantity = new InnerQuantity
                                        {
                                            Value = 133.10m,
                                            MeasurementUnitCode = "KGM",
                                        },
                                },
                        },
                };

            #endregion

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
                                                    DocumentIdentifier = "Orders123",
                                                }
                                        },
                                    DateTimePeriod = new[]
                                        {
                                            new DateTimePeriod
                                                {
                                                    DateTimePeriodGroup = new DateTimePeriodGroup
                                                        {
                                                            FormatCode = "203",
                                                            FunctionCodeQualifier = "137",
                                                            Value = "201411220000",
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
                                                                    ReferenceCodeQualifier = "ON",
                                                                    ReferenceIdentifier = "Orders123",
                                                                },
                                                        },
                                                    DateTimePeriod = new[]
                                                        {
                                                            new DateTimePeriod
                                                                {
                                                                    DateTimePeriodGroup = new DateTimePeriodGroup
                                                                        {
                                                                            Value = "201411220000",
                                                                            FunctionCodeQualifier = "171",
                                                                            FormatCode = "203",
                                                                        }
                                                                }
                                                        }
                                                },
                                            new SG1
                                                {
                                                    Reference = new Reference
                                                        {
                                                            ReferenceGroup = new ReferenceGroup
                                                                {
                                                                    ReferenceCodeQualifier = "BO",
                                                                    ReferenceIdentifier = "bo123",
                                                                },
                                                        },
                                                    DateTimePeriod = new DateTimePeriod[] {null},
                                                },
                                            new SG1
                                                {
                                                    Reference = new Reference
                                                        {
                                                            ReferenceGroup = new ReferenceGroup
                                                                {
                                                                    ReferenceCodeQualifier = "CT",
                                                                    ReferenceIdentifier = "CN1",
                                                                },
                                                        },
                                                    DateTimePeriod = new[]
                                                        {
                                                            new DateTimePeriod
                                                                {
                                                                    DateTimePeriodGroup = new DateTimePeriodGroup
                                                                        {
                                                                            FormatCode = "203",
                                                                            FunctionCodeQualifier = "171",
                                                                            Value = "201404040000",
                                                                        }
                                                                }
                                                        },
                                                },
                                            new SG1
                                                {
                                                    Reference = new Reference
                                                        {
                                                            ReferenceGroup = new ReferenceGroup
                                                                {
                                                                    ReferenceCodeQualifier = "CT",
                                                                    ReferenceIdentifier = "CN2",
                                                                },
                                                        },
                                                    DateTimePeriod = new[]
                                                        {
                                                            new DateTimePeriod
                                                                {
                                                                    DateTimePeriodGroup = new DateTimePeriodGroup
                                                                        {
                                                                            FormatCode = "203",
                                                                            FunctionCodeQualifier = "171",
                                                                            Value = "201405050000",
                                                                        }
                                                                }
                                                        },
                                                }
                                        },
                                    PartiesArray = new[]
                                        {
                                            new SG2
                                                {
                                                    NameAndAddress = new NameAndAddress
                                                        {
                                                            PartyFunctionCodeQualifier = "SU",
                                                            PartyIdentificationDetails = new PartyIdentificationDetails
                                                                {
                                                                    PartyIdentifier = "supplier gln",
                                                                },
                                                            PartyNameType = new PartyNameType
                                                                {
                                                                    PartyName = new[] {"Very long supplier name for ArraySt", "ringConverterTest"},
                                                                    PartyNameFormatCode = "UL",
                                                                },
                                                            Street = new Street
                                                                {
                                                                    StreetAndNumberOrPostBoxIdentifier = new[] {"Street; House; Flat"}
                                                                },
                                                            CountryNameCode = "643",
                                                            CityName = "City, Village",
                                                            CountrySubEntityDetails = new CountrySubEntityDetails
                                                                {
                                                                    CountrySubEntityName = "R1, D1",
                                                                },
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
                                                                                    ReferenceIdentifier = "supplier inn",
                                                                                }
                                                                        }
                                                                },
                                                            new SG3
                                                                {
                                                                    Reference = new Reference
                                                                        {
                                                                            ReferenceGroup = new ReferenceGroup
                                                                                {
                                                                                    ReferenceCodeQualifier = "GN",
                                                                                    ReferenceIdentifier = "supplier okpo",
                                                                                }
                                                                        }
                                                                },
                                                        },
                                                    FinancialInstitutionInformation = new[]
                                                        {
                                                            new FinancialInstitutionInformation
                                                                {
                                                                    PartyFunctionCodeQualifier = "BK",
                                                                    HolderIdentification = new HolderIdentificationType
                                                                        {
                                                                            HolderIdentification = "bn1",
                                                                            HolderName = new[] {"cn1"},
                                                                        },
                                                                    InstitutionIdentification = new InstitutionIdentification
                                                                        {
                                                                            InstitutionName = "Bank",
                                                                        },
                                                                },
                                                        },
                                                },
                                            new SG2
                                                {
                                                    NameAndAddress = new NameAndAddress
                                                        {
                                                            PartyFunctionCodeQualifier = "BY",
                                                            PartyIdentificationDetails = new PartyIdentificationDetails
                                                                {
                                                                    PartyIdentifier = "buyer gln",
                                                                },
                                                            PartyNameType = new PartyNameType
                                                                {
                                                                    PartyName = new[] {"Last", "First", "Middle"},
                                                                    PartyNameFormatCode = "IP",
                                                                },
                                                            Street = new Street
                                                                {
                                                                    StreetAndNumberOrPostBoxIdentifier = new[] {"Some Foreign Address"}
                                                                },
                                                            CountryNameCode = "121",
                                                            CountrySubEntityDetails = new CountrySubEntityDetails
                                                                {
                                                                },
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
                                                                                    ReferenceIdentifier = "buyer inn",
                                                                                }
                                                                        }
                                                                },
                                                            new SG3
                                                                {
                                                                    Reference = new Reference
                                                                        {
                                                                            ReferenceGroup = new ReferenceGroup
                                                                                {
                                                                                    ReferenceCodeQualifier = "GN",
                                                                                    ReferenceIdentifier = "buyer okpo",
                                                                                }
                                                                        }
                                                                },
                                                        },
                                                    FinancialInstitutionInformation = new[]
                                                        {
                                                            new FinancialInstitutionInformation
                                                                {
                                                                    HolderIdentification = new HolderIdentificationType
                                                                        {
                                                                            HolderName = new string[1],
                                                                        },
                                                                    InstitutionIdentification = new InstitutionIdentification(),
                                                                },
                                                        },
                                                },
                                        },
                                    SG13 = new[]
                                        {
                                            new SG13
                                                {
                                                    Package = new Package
                                                        {
                                                            PackageType = new PackageType(),
                                                        }
                                                }
                                        },
                                    SG28 = new[]
                                        {
                                            new SG28
                                                {
                                                    LineItem = new LineItem
                                                        {
                                                            LineItemIdentifier = "1",
                                                            ItemNumberIdentification = new ItemNumberIdentification
                                                                {
                                                                    ItemIdentifier = "GTIN 1",
                                                                },
                                                        },
                                                    AdditionalProductId = new[]
                                                        {
                                                            new AdditionalProductId
                                                                {
                                                                    ItemNumberIdentification = new[]
                                                                        {
                                                                            new ItemNumberIdentification
                                                                                {
                                                                                    ItemTypeIdentificationCode = "IN",
                                                                                    ItemIdentifier = "bid1",
                                                                                },
                                                                        },
                                                                    ProductIdentifierCodeQualifier = "z",
                                                                },
                                                            new AdditionalProductId
                                                                {
                                                                    ItemNumberIdentification = new[]
                                                                        {
                                                                            new ItemNumberIdentification(),
                                                                        },
                                                                },
                                                        },
                                                    Quantity = new[]
                                                        {
                                                            new SecondContractQuantity
                                                                {
                                                                    QuantityDetails = new QuantityDetails
                                                                        {
                                                                            Quantity = "12.34",
                                                                            MeasurementUnitCode = "PCE",
                                                                        },
                                                                },
                                                        },
                                                    DateTimePeriod = new DateTimePeriod[1],
                                                    MonetaryAmount = new[]
                                                        {
                                                            new MonetaryAmount
                                                                {
                                                                    MonetaryAmountGroup = new MonetaryAmountGroup
                                                                        {
                                                                            MonetaryAmountTypeCodeQualifier = "203",
                                                                            MonetaryAmount = "0.1200",
                                                                        }
                                                                }
                                                        },
                                                    FreeText = new FreeText[0],
                                                    GoodsIdentityNumber = new[]
                                                        {
                                                            new GoodsIdentityNumber
                                                                {
                                                                    ObjectIdentificationCodeQualifier = "BN",
                                                                    IdentityNumberRange = new []
                                                                        {
                                                                            new IdentityNumberRange
                                                                                {
                                                                                    ObjectIdentifier = new []{"SN1"},
                                                                                }
                                                                        }
                                                                }
                                                        }
                                                },
                                            new SG28
                                                {
                                                    LineItem = new LineItem
                                                        {
                                                            LineItemIdentifier = "2",
                                                            ItemNumberIdentification = new ItemNumberIdentification
                                                                {
                                                                    ItemIdentifier = "GTIN 2",
                                                                },
                                                        },
                                                    AdditionalProductId = new[]
                                                        {
                                                            new AdditionalProductId
                                                                {
                                                                    ItemNumberIdentification = new[]
                                                                        {
                                                                            new ItemNumberIdentification()
                                                                        },
                                                                },
                                                            new AdditionalProductId
                                                                {
                                                                    ItemNumberIdentification = new[]
                                                                        {
                                                                            new ItemNumberIdentification
                                                                                {
                                                                                    ItemTypeIdentificationCode = "SA",
                                                                                    ItemIdentifier = "sid2",
                                                                                },
                                                                        },
                                                                    ProductIdentifierCodeQualifier = "q",
                                                                },
                                                        },
                                                    Quantity = new[]
                                                        {
                                                            new SecondContractQuantity
                                                                {
                                                                    QuantityDetails = new QuantityDetails
                                                                        {
                                                                            Quantity = "133.10",
                                                                            MeasurementUnitCode = "KGM",
                                                                        },
                                                                },
                                                        },
                                                    DateTimePeriod = new DateTimePeriod[1],
                                                    MonetaryAmount = new[]
                                                        {
                                                            new MonetaryAmount
                                                                {
                                                                    MonetaryAmountGroup = new MonetaryAmountGroup
                                                                        {
                                                                            MonetaryAmountTypeCodeQualifier = "203",
                                                                            MonetaryAmount = "",
                                                                        }
                                                                }
                                                        },
                                                    FreeText = new []
                                                        {
                                                            new FreeText
                                                                {
                                                                    TextSubjectCodeQualifier = "DEL",
                                                                    TextLiteral = new TextLiteral{FreeTextValue = new []{"Flow type"}},
                                                                    TextReference = new TextReference
                                                                        {
                                                                            FreeTextValueCode = "ZZZ",
                                                                        },
                                                                },
                                                            new FreeText
                                                                {
                                                                    TextSubjectCodeQualifier = "ACB",
                                                                    TextLiteral = new TextLiteral{FreeTextValue = new []{"Comment"}},
                                                                    TextReference = new TextReference(),
                                                                },
                                                        },
                                                },
                                        },
                                    ControlTotal = new []
                                        {
                                            new ControlTotal
                                                {
                                                    Control = new Control
                                                        {
                                                            ControlTotalValue = "145.44",
                                                        }
                                                }
                                        }
                                },
                        }
                };

            #endregion

            var converter = converterCollection.GetConverter(new TestConverterContext());
            converter(innerDocument).Should().BeEquivalentTo(secondContractDocument);
        }

        private readonly InnerToSecondContractConverterCollection converterCollection = new InnerToSecondContractConverterCollection(
            new PathFormatterCollection(),
            new TestStringConverter());
    }
}