using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SecondOuterContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.ConverterCollections
{
    public class InnerToSecondContractConverterCollection : TestBaseConverterCollection<InnerDocument, SecondContractDocument<SecondContractDocumentBody>>
    {
        public InnerToSecondContractConverterCollection(IPathFormatterCollection pathFormatterCollection, IStringConverter stringConverter)
            : base(pathFormatterCollection, stringConverter)
        {
        }

        protected override void Configure(TestConverterContext context, ConverterConfigurator<InnerDocument, SecondContractDocument<SecondContractDocumentBody>> configurator)
        {
            Configure(configurator.GoTo(x => x.SG0[0]), context);
        }

        private void Configure(ConverterConfigurator<InnerDocument, InnerDocument, SecondContractDocument<SecondContractDocumentBody>, SecondContractDocumentBody, SecondContractDocumentBody> configurator, TestConverterContext context)
        {
            configurator.Target(message => message.BeginningOfMessage.DocumentMessageIdentification.DocumentIdentifier).Set(data => data.OrdersNumber);

            configurator.Target(message => message.DateTimePeriod[0])
                        .Set(data => data.OrdersDate, dateTime => StaticDateTimePeriodConverter.ToDateTimePeriod(dateTime, "137", "203"));

            var referencesConfigurator = configurator.GoTo(message => message.References.Each(), data => SecondContractConvertingHelpers.GetReferencesArray(data).Current());
            referencesConfigurator.SetReferenceWithDatesArray(data => data.Code, data => data.Number, data => data.Date);

            ConfigureParty(configurator.GoTo(message => message.PartiesArray[0]), data => data.Supplier, "SU", context);
            ConfigureParty(configurator.GoTo(message => message.PartiesArray[1]), data => data.Buyer, "BY", context);

            ConfigureGoodItems(configurator.GoTo(message => message.SG28.Each(), data => data.GoodItems.Current()));

            configurator.GoTo(message => message.SG13[0], data => data.Packages[0])
                        .Target(sg13 => sg13.Package.PackageType.PackageTypeDescriptionCode)
                        .Set(package => defaultConverter.Convert(package.PackageQuantity.TypeOfPackage));
            configurator.Target(message => message.ControlTotal[0].Control.ControlTotalValue)
                        .Set(data => decimalConverter.ToString(data.GoodItems.Aggregate<CommonGoodItem, decimal>(0, (current, item) => current + (item.Quantity.Value ?? 0))));
        }

        private void ConfigureParty(ConverterConfigurator<InnerDocument, InnerDocument, SecondContractDocument<SecondContractDocumentBody>, SG2, SG2> configurator, Expression<Func<InnerDocument, PartyInfo>> pathToParty, string functionCodeQualifier, TestConverterContext context)
        {
            SecondContractConvertingHelpers.ConfigureIdentifiers(configurator.GoTo(sg3 => sg3.NameAndAddress, pathToParty), functionCodeQualifier);
            SecondContractConvertingHelpers.ConfigureReferences(configurator.GoTo(sg3 => sg3.References, pathToParty));
            SecondContractConvertingHelpers.ConfigureNameAndAddress(configurator.GoTo(sg3 => sg3.NameAndAddress, pathToParty), context, defaultConverter);
            SecondContractConvertingHelpers.ConfigureFinancialInstitutionInformation(configurator.GoTo(sg3 => sg3.FinancialInstitutionInformation, pathToParty));
        }

        private void ConfigureGoodItems(ConverterConfigurator<InnerDocument, CommonGoodItem, SecondContractDocument<SecondContractDocumentBody>, SG28, SG28> configurator)
        {
            configurator.Target(sg26 => sg26.LineItem.LineItemIdentifier).Set(item => (item.CurrentIndex() + 1).ToString());
            configurator.Target(sg26 => sg26.LineItem.ItemNumberIdentification.ItemIdentifier).Set(item => item.GTIN);

            configurator.GoTo(sg26 => sg26.AdditionalProductId[0]).BatchSet(
                (additionalProductId, item) => new Batch
                    {
                        {additionalProductId.ProductIdentifierCodeQualifier, "z"},
                        {additionalProductId.ItemNumberIdentification[0].ItemTypeIdentificationCode, "IN"},
                        {additionalProductId.ItemNumberIdentification[0].ItemIdentifier.NotNull(), item.BuyerProductId}
                    }
            );

            configurator.GoTo(sg26 => sg26.AdditionalProductId[1]).BatchSet(
                (additionalProductId, item) => new Batch
                    {
                        {additionalProductId.ProductIdentifierCodeQualifier, "q"},
                        {additionalProductId.ItemNumberIdentification[0].ItemTypeIdentificationCode, "SA"},
                        {additionalProductId.ItemNumberIdentification[0].ItemIdentifier.NotNull(), item.SupplierProductId}
                    }
            );

            configurator.Target(sg26 => sg26.Quantity[0].QuantityDetails.Quantity).Set(item => decimalConverter.ToString(item.Quantity.Value));
            configurator.Target(sg26 => sg26.Quantity[0].QuantityDetails.MeasurementUnitCode).Set(item => defaultConverter.Convert(item.Quantity.MeasurementUnitCode));

            configurator.Target(sg26 => sg26.MonetaryAmount[0].MonetaryAmountGroup.MonetaryAmountTypeCodeQualifier).Set("203");
            configurator.Target(sg26 => sg26.MonetaryAmount[0].MonetaryAmountGroup.MonetaryAmount).Set(item => item.PriceSummary, s => StaticPriceFormatter.FormatPrice(s));

            configurator.Target(message => message.DateTimePeriod[0])
                        .Set(data => data.ExpireDate, dateTime => StaticDateTimePeriodConverter.ToDateTimePeriod(dateTime, "36", "102"));

            configurator.If(data => !string.IsNullOrEmpty(data.SerialNumber)).GoTo(gin => gin.GoodsIdentityNumber[0]).BatchSet(
                (gin, item) => new Batch
                    {
                        {gin.ObjectIdentificationCodeQualifier, "BN"},
                        {gin.IdentityNumberRange[0].ObjectIdentifier, ArrayStringConverter.ToArrayString(item.SerialNumber, 35, 2)}
                    });

            configurator.GoTo(sg26 => sg26.FreeText.Each(), item => item.FreeTexts(defaultConverter).ToArray().Current()).BatchSet(
                (ft, data) => new Batch
                    {
                        {ft.TextSubjectCodeQualifier, data.TextSubjectCodeQualifier},
                        {ft.TextReference.FreeTextValueCode, data.FreeTextFunctionCode},
                        {ft.TextLiteral.FreeTextValue, data.TextLiteral.FreeTextValue}
                    });
        }

        private readonly DefaultConverter defaultConverter = new DefaultConverter();
        private readonly DecimalConverter decimalConverter = new DecimalConverter("0.00");
    }
}