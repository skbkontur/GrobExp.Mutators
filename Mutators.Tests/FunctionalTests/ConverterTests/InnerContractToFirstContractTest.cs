using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

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
    public class InnerContractToFirstContractTest
    {
        [Test]
        public void Test()
        {
            #region inner document

            var inner = new InnerDocument
                {
                    FromGln = "12344321",
                    ToGln = "43211234",
                    IsTest = true,
                    OrdersNumber = "OR 431",
                    OrdersDate = new DateTime(2013, 02, 03),
                    DeliveryType = null,
                    TransportBy = "bus",
                    SumTotal = 12.34m,
                    RecadvType = TypeOfDocument.Canceled,
                    IntervalLength = 12,
                    Buyer = new PartyInfo
                        {
                            Gln = "buyer gln",
                            UsesSimplifiedTaxSystem = true,
                            PartyInfoType = PartyInfoType.Foreign,
                            ForeignPartyInfo = new ForeignPartyInfo {Name = "foreignParty"},
                            RussianPartyInfo = new RussianPartyInfo
                                {
                                    ULInfo = new UlInfo
                                        {
                                            Inn = "buyer inn",
                                            Name = "buyer name",
                                        },
                                    IPInfo = new IpInfo
                                        {
                                            Inn = "IP inn",
                                            FirstName = "Zzz",
                                            LastName = "Qxx",
                                        },
                                },
                            PartyAddress = new PartyAddress
                                {
                                    RussianAddressInfo = new RussianAddressInfo
                                        {
                                            PostalCode = "617470",
                                            City = "Moscow",
                                        },
                                    ForeignAddressInfo = new ForeignAddressInfo
                                        {
                                            Address = "Some address",
                                            CountryCode = "922",
                                        },
                                },
                            Chief = new ContactInformation
                                {
                                    Name = "Ceo name",
                                    Phone = "22747",
                                },
                            OrderContact = new ContactInformation
                                {
                                    Phone = "25016",
                                },
                        },
                    Supplier = new PartyInfo
                        {
                            Gln = "supplier gln",
                            UsesSimplifiedTaxSystem = false,
                            PartyInfoType = PartyInfoType.Russian,
                            RussianPartyInfo = new RussianPartyInfo
                                {
                                    ULInfo = new UlInfo
                                        {
                                            Inn = "supplier inn",
                                            Name = "supplier name",
                                        },
                                },
                            Chief = new ContactInformation
                                {
                                    Name = "Name",
                                    Phone = "34050",
                                },
                            OrderContact = new ContactInformation
                                {
                                    Name = "Contact name",
                                },
                        },
                    GoodItems = new[]
                        {
                            new CommonGoodItem
                                {
                                    GTIN = "131",
                                    Quantity = new Quantity
                                        {
                                            Value = 23.456m,
                                            MeasurementUnitCode = "kg",
                                        },
                                    IsReturnable = true,
                                    TypeOfUnit = "package",
                                    Marks = new[] {"123", "345", "321"},
                                    QuantityVariances = new[]
                                        {
                                            new QuantityVariance
                                                {
                                                    Reason = "some reason",
                                                    QuantityValue = 11m,
                                                },
                                        }
                                },
                            new CommonGoodItem
                                {
                                    GTIN = "222",
                                    Quantity = new Quantity
                                        {
                                            Value = 0.56m,
                                            MeasurementUnitCode = "g",
                                        },
                                    IsReturnable = false,
                                    TypeOfUnit = "package",
                                },
                        },
                };

            #endregion

            #region expected document

            var expectedDocument = new FirstContractDocument
                {
                    Header = new FirstContractDocumentHeader
                        {
                            Sender = "12344321",
                            Recipient = "43211234",
                            IsTest = "1",
                        },
                    Document = new[]
                        {
                            new FirstContractDocumentBody
                                {
                                    OriginOrder = new DocumentIdentificator
                                        {
                                            Number = "OR 431",
                                            Date = new DateTime(2013, 02, 03),
                                        },
                                    DeliveryType = null,
                                    DeliveryInfo = new DeliveryInfo {TransportBy = "bus"},
                                    IntervalLength = "12",
                                    Status = "canceled",
                                    Buyer = new FirstContractPartyInfo
                                        {
                                            Gln = "buyer gln",
                                            TaxSystem = "Simplified",
                                            ForeignAddress = new ForeignAddress
                                                {
                                                    Address = "Some address",
                                                    CountryIsoCode = "922",
                                                },
                                            Organization = new Organization
                                                {
                                                    Inn = "buyer inn",
                                                    Name = "buyer name",
                                                },
                                            SelfEmployed = new SelfEmployed
                                                {
                                                    Inn = "IP inn",
                                                    FullName = new FullName
                                                        {
                                                            FirstName = "Zzz",
                                                            LastName = "Qxx",
                                                        },
                                                },
                                            ForeignOrganization = new ForeignOrganization
                                                {
                                                    Name = "foreignParty",
                                                },
                                            RussianAddress = new RussianAddress
                                                {
                                                    City = "Moscow",
                                                    PostalCode = "617470",
                                                },
                                            ContactInfo = new ContactInfo
                                                {
                                                    Ceo = new ContactPersonInfo
                                                        {
                                                            Name = "Ceo name",
                                                            Phone = "22747",
                                                        }
                                                },
                                            AdditionalInfo = new AdditionalInfo
                                                {
                                                    Phone = "25016",
                                                    NameOfCeo = "Ceo name",
                                                }
                                        },
                                    Seller = new FirstContractPartyInfo
                                        {
                                            Gln = "supplier gln",
                                            TaxSystem = null,
                                            Organization = new Organization
                                                {
                                                    Inn = "supplier inn",
                                                    Name = "supplier name",
                                                },
                                            ForeignAddress = new ForeignAddress(),
                                            SelfEmployed = new SelfEmployed {FullName = new FullName()},
                                            RussianAddress = new RussianAddress(),
                                            ContactInfo = new ContactInfo
                                                {
                                                    Ceo = new ContactPersonInfo
                                                        {
                                                            Name = "Name",
                                                            Phone = "34050",
                                                        }
                                                },
                                            AdditionalInfo = new AdditionalInfo
                                                {
                                                    Phone = "34050",
                                                    NameOfCeo = "Contact name",
                                                }
                                        },
                                    LineItems = new LineItems
                                        {
                                            TotalSumExcludingTaxes = "12.34",
                                            LineItem = new[]
                                                {
                                                    new LineItem
                                                        {
                                                            Gtin = "131",
                                                            OrderedQuantity = new MeasureUnitQuantity
                                                                {
                                                                    Quantity = "23.46",
                                                                    UnitOfMeasure = "kg",
                                                                },
                                                            TypeOfUnit = "RET",
                                                            ControlMarks = new[] {"123", "345", "321"},
                                                            ToBeReturnedQuantity = new[]
                                                                {
                                                                    new ReasonQuantity
                                                                        {
                                                                            Quantity = "11.00",
                                                                            ReasonOfReturn = "some reason",
                                                                        }
                                                                },
                                                            Declarations = new string[0],
                                                        },
                                                    new LineItem
                                                        {
                                                            Gtin = "222",
                                                            OrderedQuantity = new MeasureUnitQuantity
                                                                {
                                                                    Quantity = "0.56",
                                                                    UnitOfMeasure = "g",
                                                                },
                                                            TypeOfUnit = "package",
                                                            ControlMarks = new string[0],
                                                            ToBeReturnedQuantity = new ReasonQuantity[0],
                                                            Declarations = new string[0],
                                                        },
                                                },
                                        },
                                }
                        }
                };

            #endregion

            var converter = converterCollection.GetConverter(new TestConverterContext());
            var convertedDocument = converter(inner);

            convertedDocument.Should().BeEquivalentTo(expectedDocument, config => config
                                                                            .Excluding(x => x.Document[0].Comment)
                                                                            .Excluding(x => x.Header.CreationDateTime)
                                                                            .Excluding(x => x.CreationDateTime)
                                                                            .Excluding(x => x.Document[0].MessageCodes));
        }

        [Test]
        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public void TestCalculatingConstants()
        {
            var converter = converterCollection.GetConverter(new TestConverterContext());
            var innerDocument = new InnerDocument();

            var firstResult = converter(innerDocument);
            Thread.Sleep(100);
            var secondResult = converter(innerDocument);

            firstResult.Document[0].Comment.Should().NotBe(secondResult.Document[0].Comment);
            firstResult.Document[0].MessageCodes[0].Should().Be(secondResult.Document[0].MessageCodes[0]);

            firstResult.Header.CreationDateTime.Value.Should().NotBe(secondResult.Header.CreationDateTime.Value);
            firstResult.CreationDateTime.Value.Should().Be(secondResult.CreationDateTime.Value);
        }

        private readonly InnerContractToFirstContractConverterCollection converterCollection = new InnerContractToFirstContractConverterCollection(
            new PathFormatterCollection(),
            new DefaultConverter(),
            new DecimalConverter("0.00")
            );
    }
}