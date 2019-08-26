using System;

using FluentAssertions;

using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.ConverterCollections;
using Mutators.Tests.FunctionalTests.FirstOuterContract;
using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

using NUnit.Framework;

namespace Mutators.Tests.FunctionalTests.ConverterTests
{
    [TestFixture]
    public class FirstContractToInnerContractTest
    {
        [Test]
        public void TestNoneContext()
        {
            #region first contract document

            var firstContractDocument = new FirstContractDocument
                {
                    Id = "id_dqd",
                    Header = new FirstContractDocumentHeader
                        {
                            Sender = "sender",
                            Recipient = "recipient",
                            IsTest = "1",
                            CreationDateTime = null,
                        },
                    CreationDateTime = new DateTime(2012, 02, 02),
                    Document = new[]
                        {
                            new FirstContractDocumentBody
                                {
                                    OriginOrder = new DocumentIdentificator
                                        {
                                            Number = "OR123",
                                            Date = new DateTime(2012, 01, 01),
                                        },
                                    DeliveryType = "Paper",
                                    DeliveryInfo = new DeliveryInfo
                                        {
                                            TransportBy = "transportBy",
                                        },
                                    Status = "canceled",
                                    IntervalLength = "21",
                                    Buyer = new FirstContractPartyInfo
                                        {
                                            Gln = "12345678",
                                            SelfEmployed = new SelfEmployed
                                                {
                                                    Inn = "inn",
                                                    FullName = new FullName
                                                        {
                                                            FirstName = "First Name",
                                                            LastName = "Last name",
                                                        }
                                                },
                                            Organization = null,
                                            ForeignOrganization = null,
                                            ForeignAddress = new ForeignAddress {Address = null, CountryIsoCode = null},
                                            TaxSystem = "Simplified",
                                            RussianAddress = new RussianAddress
                                                {
                                                    PostalCode = "620142",
                                                    City = "Ekat",
                                                },
                                            ContactInfo = new ContactInfo
                                                {
                                                    Ceo = new ContactPersonInfo
                                                        {
                                                            Name = "Jeff",
                                                            Phone = "8909"
                                                        }
                                                }
                                        },
                                    Seller = new FirstContractPartyInfo
                                        {
                                            Gln = "87654321",
                                            SelfEmployed = null,
                                            Organization = new Organization
                                                {
                                                    Inn = "org inn",
                                                    Name = "org name",
                                                },
                                            ForeignOrganization = new ForeignOrganization
                                                {
                                                    Name = "foreign org name",
                                                },
                                            ForeignAddress = new ForeignAddress
                                                {
                                                    Address = "221B, Baker st., London",
                                                    CountryIsoCode = "111",
                                                },
                                            TaxSystem = "complex",
                                            RussianAddress = null,
                                            ContactInfo = new ContactInfo
                                                {
                                                    Ceo = null,
                                                },
                                            AdditionalInfo = new AdditionalInfo
                                                {
                                                    NameOfCeo = "Bezos",
                                                    Phone = "8808",
                                                }
                                        },
                                    LineItems = new LineItems
                                        {
                                            TotalSumExcludingTaxes = "123.45",
                                            LineItem = new[]
                                                {
                                                    new LineItem
                                                        {
                                                            Gtin = "134143",
                                                            OrderedQuantity = new MeasureUnitQuantity
                                                                {
                                                                    Quantity = "0.43",
                                                                    UnitOfMeasure = "kg",
                                                                },
                                                            TypeOfUnit = "RET",
                                                            ControlMarks = new[] {"abc", "def", "zzz"},
                                                            Declarations = new[] {"123", "345", "567", "exclude"},
                                                            ToBeReturnedQuantity = new[]
                                                                {
                                                                    new ReasonQuantity
                                                                        {
                                                                            ReasonOfReturn = "bad goods",
                                                                            Quantity = "92",
                                                                        },
                                                                    new ReasonQuantity
                                                                        {
                                                                            Quantity = "23",
                                                                            ReasonOfReturn = "no need"
                                                                        }
                                                                },
                                                            FlowType = "very flow",
                                                        },
                                                    new LineItem
                                                        {
                                                            Gtin = "908452",
                                                            OrderedQuantity = new MeasureUnitQuantity
                                                                {
                                                                    Quantity = "3",
                                                                    UnitOfMeasure = null,
                                                                },
                                                            TypeOfUnit = "Pack",
                                                            Declarations = null,
                                                            ToBeReturnedQuantity = new[]
                                                                {
                                                                    new ReasonQuantity
                                                                        {
                                                                            ReasonOfReturn = "no money",
                                                                            Quantity = "11",
                                                                        },
                                                                    new ReasonQuantity
                                                                        {
                                                                            Quantity = "33",
                                                                            ReasonOfReturn = "too much"
                                                                        }
                                                                },
                                                            FlowType = "so flow",
                                                        }
                                                }
                                        },
                                    MessageCodes = new []{"nullify", "azb", "czf"}
                                },
                        }
                };

            #endregion

            #region inner document

            var innerDocument = new InnerDocument
                {
                    FromGln = "sender",
                    ToGln = "recipient",
                    IsTest = true,
                    CreationDateTimeBySender = new DateTime(2012, 02, 02),
                    OrdersNumber = "OR123",
                    OrdersDate = new DateTime(2012, 01, 01),
                    DeliveryType = DocumentsDeliveryType.Paper,
                    TransportBy = "transportBy",
                    RecadvType = TypeOfDocument.Canceled,
                    SumTotal = 123.45m,
                    FlowType = "very flow",
                    IntervalLength = 21,
                    Buyer = new PartyInfo
                        {
                            Gln = "12345678",
                            PartyInfoType = PartyInfoType.Russian,
                            PartyAddress = new PartyAddress
                                {
                                    AddressType = AddressType.Russian,
                                    RussianAddressInfo = new RussianAddressInfo
                                        {
                                            PostalCode = "620142",
                                            City = "Ekat",
                                        }
                                },
                            UsesSimplifiedTaxSystem = true,
                            RussianPartyInfo = new RussianPartyInfo
                                {
                                    RussianPartyType = RussianPartyType.IP,
                                    IPInfo = new IpInfo
                                        {
                                            Inn = "inn",
                                            FirstName = "First Name",
                                            LastName = "Last name",
                                        }
                                },
                            Chief = new ContactInformation
                                {
                                    Name = "Jeff",
                                    Phone = "8909",
                                }
                        },
                    Supplier = new PartyInfo
                        {
                            Gln = "87654321",
                            PartyInfoType = PartyInfoType.Foreign,
                            PartyAddress = new PartyAddress
                                {
                                    AddressType = AddressType.Foreign,
                                    ForeignAddressInfo = new ForeignAddressInfo
                                        {
                                            CountryCode = "111",
                                            Address = "221B, Baker st., London",
                                        }
                                },
                            UsesSimplifiedTaxSystem = false,
                            RussianPartyInfo = new RussianPartyInfo
                                {
                                    RussianPartyType = RussianPartyType.UL,
                                    ULInfo = new UlInfo
                                        {
                                            Inn = "org inn",
                                            Name = "org name",
                                        }
                                },
                            Chief = new ContactInformation
                                {
                                    Name = "Bezos",
                                    Phone = "8808",
                                }
                        },
                    GoodItems = new[]
                        {
                            new CommonGoodItem
                                {
                                    GTIN = "134143",
                                    Quantity = new Quantity
                                        {
                                            Value = 0.43m,
                                            MeasurementUnitCode = "kg",
                                        },
                                    GoodNumber = new GoodNumber
                                        {
                                            Number = 1,
                                            MessageType = MessageType.ReceivingAdvice,
                                        },
                                    IsReturnable = true,
                                    Marks = new[] {"abc", "def", "zzz"},
                                    Declarations = new[]
                                        {
                                            new CustomDeclaration {Number = "123"},
                                            new CustomDeclaration {Number = "345"},
                                            new CustomDeclaration {Number = "567"},
                                            new CustomDeclaration {Number = "exclude"},
                                        },
                                    QuantityVariances = new[]
                                        {
                                            new QuantityVariance
                                                {
                                                    Reason = "bad goods",
                                                    QuantityValue = 92m,
                                                },
                                            new QuantityVariance
                                                {
                                                    Reason = "no need",
                                                    QuantityValue = 23m,
                                                },
                                        },
                                    ExcludeFromSummation = true,
                                },
                            new CommonGoodItem
                                {
                                    GTIN = "908452",
                                    Quantity = new Quantity
                                        {
                                            Value = 3m,
                                            MeasurementUnitCode = "PCE",
                                        },
                                    GoodNumber = new GoodNumber
                                        {
                                            Number = 2,
                                            MessageType = MessageType.ReceivingAdvice,
                                        },
                                    IsReturnable = false,
                                    Marks = new string[0],
                                    Declarations = new CustomDeclaration[0],
                                    QuantityVariances = new[]
                                        {
                                            new QuantityVariance
                                                {
                                                    Reason = "no money",
                                                    QuantityValue = 11m,
                                                },
                                            new QuantityVariance
                                                {
                                                    Reason = "too much",
                                                    QuantityValue = 33m,
                                                },
                                        },
                                },
                        },
                    Nullify = true,
                    MessageCodes = new []{"nullify", null, "czf"},
                };

            #endregion

            var converter = converterCollection.GetConverter(new TestConverterContext());
            converter(firstContractDocument).Should().BeEquivalentTo(innerDocument);
        }

        [Test]
        public void TestReconfigureForContextA()
        {
            #region first contract document

            var firstContractDocument = new FirstContractDocument
                {
                    Id = "id_dqd",
                    Header = new FirstContractDocumentHeader
                        {
                            Sender = "sender",
                            Recipient = "recipient",
                            IsTest = "1",
                            CreationDateTime = null,
                        },
                    CreationDateTime = new DateTime(2012, 02, 02),
                    Document = new[]
                        {
                            new FirstContractDocumentBody
                                {
                                    OriginOrder = new DocumentIdentificator
                                        {
                                            Number = "OR123",
                                            Date = new DateTime(2012, 01, 01),
                                        },
                                    DeliveryType = "Paper",
                                    DeliveryInfo = new DeliveryInfo
                                        {
                                            TransportBy = "transportBy",
                                        },
                                    Status = "canceled",
                                    IntervalLength = "21",
                                    Buyer = new FirstContractPartyInfo
                                        {
                                            Gln = "12345678",
                                            SelfEmployed = new SelfEmployed
                                                {
                                                    Inn = "inn",
                                                    FullName = new FullName
                                                        {
                                                            FirstName = "First Name",
                                                            LastName = "Last name",
                                                        }
                                                },
                                            Organization = null,
                                            ForeignOrganization = null,
                                            ForeignAddress = new ForeignAddress {Address = null, CountryIsoCode = null},
                                            TaxSystem = "Simplified",
                                            RussianAddress = new RussianAddress
                                                {
                                                    PostalCode = "620142",
                                                    City = "Ekat",
                                                },
                                            ContactInfo = new ContactInfo
                                                {
                                                    Ceo = new ContactPersonInfo
                                                        {
                                                            Name = "Jeff",
                                                            Phone = "8909"
                                                        }
                                                }
                                        },
                                    Seller = new FirstContractPartyInfo
                                        {
                                            Gln = "87654321",
                                            SelfEmployed = null,
                                            Organization = new Organization
                                                {
                                                    Inn = "org inn",
                                                    Name = "org name",
                                                },
                                            ForeignOrganization = new ForeignOrganization
                                                {
                                                    Name = "foreign org name",
                                                },
                                            ForeignAddress = new ForeignAddress
                                                {
                                                    Address = "221B, Baker st., London",
                                                    CountryIsoCode = "111",
                                                },
                                            TaxSystem = "complex",
                                            RussianAddress = null,
                                            ContactInfo = new ContactInfo
                                                {
                                                    Ceo = null,
                                                },
                                            AdditionalInfo = new AdditionalInfo
                                                {
                                                    NameOfCeo = "Bezos",
                                                    Phone = "8808",
                                                }
                                        },
                                    LineItems = new LineItems
                                        {
                                            TotalSumExcludingTaxes = "123.45",
                                            LineItem = new[]
                                                {
                                                    new LineItem
                                                        {
                                                            Gtin = "134143",
                                                            OrderedQuantity = new MeasureUnitQuantity
                                                                {
                                                                    Quantity = "0.43",
                                                                    UnitOfMeasure = "kg",
                                                                },
                                                            TypeOfUnit = "RET",
                                                            ControlMarks = new[] {"abc", "def", "zzz"},
                                                            Declarations = new[] {"123", "345", "567"},
                                                            ToBeReturnedQuantity = new[]
                                                                {
                                                                    new ReasonQuantity
                                                                        {
                                                                            ReasonOfReturn = "bad goods",
                                                                            Quantity = "92",
                                                                        },
                                                                    new ReasonQuantity
                                                                        {
                                                                            Quantity = "23",
                                                                            ReasonOfReturn = "no need"
                                                                        }
                                                                },
                                                            FlowType = "very flow",
                                                        },
                                                }
                                        },
                                    MessageCodes = new []{"nullify", "azb", "czf"}
                                },
                        }
                };

            #endregion

            #region inner document

            var innerDocument = new InnerDocument
                {
                    FromGln = "recipient",
                    ToGln = "sender",
                    IsTest = true,
                    CreationDateTimeBySender = new DateTime(2012, 02, 02),
                    OrdersNumber = "OR123",
                    OrdersDate = new DateTime(2012, 01, 01),
                    DeliveryType = DocumentsDeliveryType.Paper,
                    TransportBy = "transportBy",
                    RecadvType = TypeOfDocument.Canceled,
                    SumTotal = 123.45m,
                    FlowType = "very flow",
                    IntervalLength = 21,
                    Buyer = new PartyInfo
                        {
                            Gln = "12345678",
                            PartyInfoType = PartyInfoType.Russian,
                            PartyAddress = new PartyAddress
                                {
                                    AddressType = AddressType.Russian,
                                    RussianAddressInfo = new RussianAddressInfo
                                        {
                                            PostalCode = "620142",
                                            City = "Ekat",
                                        }
                                },
                            UsesSimplifiedTaxSystem = true,
                            RussianPartyInfo = new RussianPartyInfo
                                {
                                    RussianPartyType = RussianPartyType.IP,
                                    IPInfo = new IpInfo
                                        {
                                            Inn = "inn",
                                            FirstName = "First Name",
                                            LastName = "Last name",
                                        }
                                },
                            Chief = new ContactInformation
                                {
                                    Name = "Jeff",
                                    Phone = "8909",
                                }
                        },
                    Supplier = new PartyInfo
                        {
                            Gln = "87654321",
                            PartyInfoType = PartyInfoType.Foreign,
                            PartyAddress = new PartyAddress
                                {
                                    AddressType = AddressType.Foreign,
                                    ForeignAddressInfo = new ForeignAddressInfo
                                        {
                                            CountryCode = "111",
                                            Address = "221B, Baker st., London",
                                        }
                                },
                            UsesSimplifiedTaxSystem = false,
                            RussianPartyInfo = new RussianPartyInfo
                                {
                                    RussianPartyType = RussianPartyType.UL,
                                    ULInfo = new UlInfo
                                        {
                                            Inn = "org inn",
                                            Name = "org name",
                                        }
                                },
                            Chief = new ContactInformation
                                {
                                    Name = "Bezos",
                                    Phone = "8808",
                                }
                        },
                    GoodItems = new[]
                        {
                            new CommonGoodItem
                                {
                                    GTIN = "134143",
                                    Quantity = new Quantity
                                        {
                                            Value = 0.43m,
                                            MeasurementUnitCode = "kg",
                                        },
                                    GoodNumber = new GoodNumber
                                        {
                                            Number = 1,
                                            MessageType = MessageType.ReceivingAdvice,
                                        },
                                    IsReturnable = true,
                                    Marks = new[] {"123", "345", "567"}, 
                                    Declarations = new[]
                                        {
                                            new CustomDeclaration {Number = "abc"},
                                            new CustomDeclaration {Number = "def"},
                                            new CustomDeclaration {Number = "zzz"},
                                        },
                                    QuantityVariances = new[]
                                        {
                                            new QuantityVariance
                                                {
                                                    Reason = "bad goods",
                                                    QuantityValue = 92m,
                                                },
                                            new QuantityVariance
                                                {
                                                    Reason = "no need",
                                                    QuantityValue = 23m,
                                                },
                                        },
                                }
                        },
                    Nullify = true,
                    MessageCodes = new []{"nullify", null, "czf"},
                };

            #endregion

            var converter = converterCollection.GetConverter(new TestConverterContext(MutatorsContextType.A));
            converter(firstContractDocument).Should().BeEquivalentTo(innerDocument);

        }

        private readonly FirstContractToInnerContractConverterCollection converterCollection = new FirstContractToInnerContractConverterCollection(
            new PathFormatterCollection(),
            new DefaultConverter(),
            new DecimalConverter("0.000")
            );
    }
}