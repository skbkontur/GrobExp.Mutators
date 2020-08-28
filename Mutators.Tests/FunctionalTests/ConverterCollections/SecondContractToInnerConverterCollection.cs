using System.Collections.Generic;
using System.Linq;

using GrobExp.Mutators;
using GrobExp.Mutators.Validators.Texts;

using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SecondOuterContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.ConverterCollections
{
    public class SecondContractToInnerConverterCollection : TestBaseConverterCollection<SecondContractDocument<SecondContractDocumentBody>, InnerDocument>
    {
        public SecondContractToInnerConverterCollection(IPathFormatterCollection pathFormatterCollection, IStringConverter stringConverter)
            : base(pathFormatterCollection, stringConverter)
        {
        }

        protected override void Configure(TestConverterContext converterContext, ConverterConfigurator<SecondContractDocument<SecondContractDocumentBody>, InnerDocument> configurator)
        {
            var subConfigurator = configurator.GoTo(data => data, ordersInterchange => ordersInterchange.SG0.Single());
            Configure(subConfigurator);
        }

        private void Configure(ConverterConfigurator<SecondContractDocument<SecondContractDocumentBody>, SecondContractDocumentBody, InnerDocument, InnerDocument, InnerDocument> subConfigurator)
        {
            subConfigurator.Target(data => data.OrdersNumber).Set(message => message.BeginningOfMessage.DocumentMessageIdentification.DocumentIdentifier);
            subConfigurator.ConfigureDateOrUtcNow("137", data => data.OrdersDate);
            subConfigurator.ConfigureDate("50", data => data.ReceivingDate);

            subConfigurator.Target(data => data.CurrencyCode).Set(message => (from details in message.Currency.CurrencyDetails
                                                                              where details.UsageCodeQualifier == "2" && details.TypeCodeQualifier == "4"
                                                                              select details.IdentificationCode).FirstOrDefault(),
                                                                  s => defaultConverter.Convert(s));

            subConfigurator.Target(data => data.FlowType).Set(message => message.FreeText.Any(y => y.TextSubjectCodeQualifier == "DEL")
                                                                             ? defaultConverter.Convert(message.FreeText.FirstOrDefault(y => y.TextSubjectCodeQualifier == "DEL").TextReference.FreeTextValueCode)
                                                                             : message.FreeText.Any(y => y.TextSubjectCodeQualifier == "ZZZ" && y.TextLiteral.FreeTextValue[0] == "Fresh")
                                                                                 ? "fresh"
                                                                                 : defaultConverter.Convert(message.SG28.FirstOrDefault().FreeText.FirstOrDefault(y => y.TextSubjectCodeQualifier == "DEL" && y.TextReference.CodeListResponsibleAgencyCode == "ZZZ").TextReference.FreeTextValueCode));

            subConfigurator.Target(data => data.TransportDetails.VehicleNumber)
                           .Set(message => message.SG10.FirstOrDefault(sg10 => sg10.DetailsOfTransport.TransportStageCodeQualifier == "1").DetailsOfTransport.TransportIdentification.TransportMeansIdentificationName);

            subConfigurator.Target(data => data.FreeText).Set(message => ArrayStringConverter.ToString(message.FreeText.FirstOrDefault(freeText => freeText.TextSubjectCodeQualifier == "ZZZ" || freeText.TextSubjectCodeQualifier == "PUR" || freeText.TextSubjectCodeQualifier == "AAI").TextLiteral.FreeTextValue));

            subConfigurator.GoTo(x => x, x => x.References).ConfigureReference("BO", data => data.BlanketOrdersNumber);

            subConfigurator.If(message => !string.IsNullOrEmpty(message.ControlTotal.FirstOrDefault(cnt => cnt.Control.ControlTotalTypeCodeQualifier == "11").Control.ControlTotalValue))
                           .Target(data => data.OrdersTotalPackageQuantity)
                           .Set(message => decimalConverter.ToDecimal(message.ControlTotal.FirstOrDefault(cnt => cnt.Control.ControlTotalTypeCodeQualifier == "11").Control.ControlTotalValue));

            subConfigurator.ConfigureMonetaryAmountsInfo(new MonetaryAmountConfig<InnerDocument>("79", x => x.RecadvTotal),
                                                         new MonetaryAmountConfig<InnerDocument>(new[] {"77", "9"}, x => x.TotalWithVAT));

            subConfigurator.GoTo(x => x.DespatchParties.Each().PartyInfo, message => message.PartiesArray.Where(sg2 => sg2.NameAndAddress.PartyFunctionCodeQualifier == "PW").Current())
                           .ConfigureParty<SecondContractDocument<SecondContractDocumentBody>, InnerDocument, SG2, SG3, SG5>();

            subConfigurator.GoTo(data => data.Transports.Each(),
                                 message => message.SG10.Where(sg10 => sg10.DetailsOfTransport.TransportStageCodeQualifier == "20")
                                                   .SelectMany(sg10 => DefaultIfNullOrEmpty(sg10.SG11),
                                                               (sg10, sg11) => new
                                                                   {
                                                                       sg10.DetailsOfTransport.TransportMeans.TransportMeansDescription,
                                                                       TypeOfTransportCode = defaultConverter.Convert(sg10.DetailsOfTransport.TransportMeans.TransportMeansDescriptionCode),
                                                                       sg11.DateTimePeriod
                                                                   })
                                                   .Where(x => !string.IsNullOrEmpty(x.TypeOfTransportCode) || !string.IsNullOrEmpty(x.TransportMeansDescription) || x.DateTimePeriod != null)
                                                   .Current())
                           .BatchSet((x, y) => new Batch
                               {
                                   {x.TypeOfTransport, y.TransportMeansDescription},
                                   {x.TypeOfTransportCode, y.TypeOfTransportCode},
                                   {x.DeliveryDateForVehicle, dateTimePeriodConverter.ToDateTime(y.DateTimePeriod.FirstOrDefault(period => period.DateTimePeriodGroup.FunctionCodeQualifier == "232").DateTimePeriodGroup)},
                               });

            ConfigureGoodItems(subConfigurator.GoTo(data => data.GoodItems.Each(), message => message.SG28.Current()));
        }

        private void ConfigureGoodItems(ConverterConfigurator<SecondContractDocument<SecondContractDocumentBody>, SG28, InnerDocument, CommonGoodItem, CommonGoodItem> configurator)
        {
            configurator.Target(goodItem => goodItem.GoodNumber.Number).Set(sg28 => sg28.CurrentIndex() + 1);
            configurator.ConfigureCommonGoodItemInfo<SecondContractDocument<SecondContractDocumentBody>, SG28, InnerDocument, CommonGoodItem>();
            configurator.ConfigureMonetaryAmountsInfo(new MonetaryAmountConfig<CommonGoodItem>("161", x => x.ExciseTax));
            configurator.ConfigureCountiesOfOrigin<SecondContractDocument<SecondContractDocumentBody>, SG28, InnerDocument>();
            ConfigurePackages(configurator.GoTo(x => x.Packages.Each(), sg28 => sg28.SG34.Each()));
        }

        private void ConfigurePackages(ConverterConfigurator<SecondContractDocument<SecondContractDocumentBody>, SG34, InnerDocument, PackageForItem, PackageForItem> configurator)
        {
            configurator.Target(x => x.PackageTypeCode)
                        .Set(sg34 => sg34.Package.PackageType.PackageTypeDescriptionCode,
                             s => defaultConverter.ConvertWithDefault(s, "default"),
                             s => s == null,
                             s => new ValueMustBelongToText {Value = s});
            configurator.Target(x => x.Quantity).Set(sg34 => decimalConverter.ToDecimal(sg34.Package.PackageQuantity));

            var subConfigurator = configurator.GoTo(x => x.OnePackageQuantity,
                                                    sg34 => sg34.Quantity.FirstOrDefault(x => x.QuantityDetails.QuantityTypeCodeQualifier == "52").QuantityDetails);
            subConfigurator.Target(x => x.Value).Set(x => decimalConverter.ToDecimal(x.Quantity));
            subConfigurator.If(x => defaultConverter.ConvertWithDefault(x.MeasurementUnitCode, "DEFAULT") == null
                                    && decimalConverter.ToDecimal(x.Quantity).HasValue)
                           .Target(x => x.MeasurementUnitCode)
                           .Set("DME");
            subConfigurator.If(x => defaultConverter.ConvertWithDefault(x.MeasurementUnitCode, "DEFAULT") != null)
                           .Target(x => x.MeasurementUnitCode)
                           .Set(x => defaultConverter.Convert(x.MeasurementUnitCode));
        }

        private T[] DefaultIfNullOrEmpty<T>(IEnumerable<T> source)
        {
            return (source ?? new T[0]).DefaultIfEmpty().ToArray();
        }

        private readonly DefaultConverter defaultConverter = new DefaultConverter();
        private readonly DecimalConverter decimalConverter = new DecimalConverter("0.00");
        private readonly DateTimePeriodConverter dateTimePeriodConverter = new DateTimePeriodConverter(new DateTimeConvertersCollection());
    }
}