using System.Linq;

using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.FirstOuterContract;
using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.ConverterCollections
{
    public class FirstContractToInnerContractConverterCollection : TestBaseConverterCollection<FirstContractDocument, InnerDocument>
    {
        public FirstContractToInnerContractConverterCollection(
            IPathFormatterCollection pathFormatterCollection,
            DefaultConverter defaultConverter,
            DecimalConverter decimalConverter)
            : base(pathFormatterCollection, new EnumStringConverter())
        {
            this.defaultConverter = defaultConverter;
            this.decimalConverter = decimalConverter;
        }

        protected override void Configure(TestConverterContext converterContext, ConverterConfigurator<FirstContractDocument, InnerDocument> configurator)
        {
            Configure(configurator);
            switch (converterContext.MutatorsContextType)
            {
            case MutatorsContextType.A:
                ReconfigureForA(configurator);
                break;
            }
        }

        private void Configure(ConverterConfigurator<FirstContractDocument, InnerDocument> configurator)
        {
            configurator.Target(x => x.FromGln).Set(x => x.Header.Sender);
            configurator.Target(x => x.ToGln).Set(x => x.Header.Recipient);

            configurator.Target(x => x.IsTest).Set(x => x.Header.IsTest == "1");
            configurator.Target(x => x.CreationDateTimeBySender).Set(x => x.Header.CreationDateTime ?? x.CreationDateTime);

            var subConfigurator = configurator.GoTo(x => x, x => x.Document[0]);

            subConfigurator.Target(x => x.OrdersNumber).Set(x => x.OriginOrder.Number);
            subConfigurator.Target(x => x.OrdersDate).Set(x => x.OriginOrder.Date);

            subConfigurator.Target(x => x.DeliveryType).Set(x => x.DeliveryType.ParseNullableEnum<DocumentsDeliveryType>());

            subConfigurator.Target(x => x.TransportBy).Set(x => x.DeliveryInfo.TransportBy, x => defaultConverter.Convert(x));

            subConfigurator.Target(x => x.SumTotal).Set(x => decimalConverter.ToDecimal(x.LineItems.TotalSumExcludingTaxes));
            subConfigurator.Target(x => x.RecadvType)
                           .If(x => !string.IsNullOrEmpty(x.Status) && x.Status.Contains("canceled"))
                           .Set(x => TypeOfDocument.Canceled);
            
            subConfigurator.Target(x => x.FlowType).Set(x => x.Comment == "Fresh" ? "fresh" : defaultConverter.Convert(x.LineItems.LineItem.FirstOrDefault().FlowType));

            subConfigurator.Target(x => x.IntervalLength).Set(x => x.IntervalLength.ParseNullableInt());

            PartyInfoConfigurators.ConfigureFromFirstContractToInner(subConfigurator.GoTo(x => x.Buyer, x => x.Buyer), defaultConverter);
            PartyInfoConfigurators.ConfigureFromFirstContractToInner(subConfigurator.GoTo(x => x.Supplier, x => x.Seller), defaultConverter);

            ConfigureGoodItems(subConfigurator.GoTo(x => x.GoodItems.Each(), x => x.LineItems.LineItem.Current()));

            subConfigurator.Target(x => x.MessageCodes.Each()).Set(x => x.MessageCodes.Current());
            subConfigurator.Target(x => x.Nullify).Set(x => x.MessageCodes[0] == "nullify");
            subConfigurator.Target(x => x.MessageCodes[1]).NullifyIf(x => x.Nullify);
        }

        private void ConfigureGoodItems(ConverterConfigurator<FirstContractDocument, LineItem, InnerDocument, CommonGoodItem, CommonGoodItem> configurator)
        {
            configurator.Target(x => x.GoodNumber.Number).Set(x => x.CurrentIndex() + 1);
            configurator.Target(x => x.GoodNumber.MessageType).Set(MessageType.ReceivingAdvice);
            configurator.Target(x => x.GTIN).Set(x => x.Gtin);

            configurator.Target(x => x.Quantity.Value).Set(x => decimalConverter.ToDecimal(x.OrderedQuantity.Quantity));
            configurator.Target(x => x.Quantity.MeasurementUnitCode)
                        .If(x => defaultConverter.Convert(x.OrderedQuantity.UnitOfMeasure) != null)
                        .Set(x => defaultConverter.Convert(x.OrderedQuantity.UnitOfMeasure));
            configurator.Target(x => x.Quantity.MeasurementUnitCode)
                        .If(x => defaultConverter.Convert(x.OrderedQuantity.UnitOfMeasure) == null)
                        .Set(x => "PCE");
            
            configurator.Target(x => x.IsReturnable).Set(x => x.TypeOfUnit == "RET");

            configurator.Target(x => x.Marks.Each()).Set(x => x.ControlMarks.Current());
            configurator.Target(x => x.Declarations.Each().Number).Set(x => defaultConverter.Convert(x.Declarations.Current()));
            configurator.Target(x => x.ExcludeFromSummation).Set(x => x.Declarations.Any(y => y.Contains("exclude")));

            ConfigureQuantityVariances(configurator.GoTo(x => x.QuantityVariances.Each(), x => x.ToBeReturnedQuantity.Current()));
        }

        private void ConfigureQuantityVariances(ConverterConfigurator<FirstContractDocument, ReasonQuantity, InnerDocument, QuantityVariance, QuantityVariance> configurator)
        {
            configurator.Target(x => x.Reason).Set(x => x.ReasonOfReturn);
            configurator.Target(x => x.QuantityValue).Set(x => decimalConverter.ToDecimal(x.Quantity));
        }

        private void ReconfigureForA(ConverterConfigurator<FirstContractDocument,InnerDocument> configurator)
        {
            configurator.Target(x => x.FromGln).Set(x => x.Header.Recipient);
            configurator.Target(x => x.ToGln).Set(x => x.Header.Sender);
            configurator.Target(x => x.GoodItems.Each().Marks.Each()).Set(x => x.Document[0].LineItems.LineItem.Current().Declarations.Each());
            configurator.Target(x => x.GoodItems.Each().Declarations.Each().Number).Set(x => x.Document[0].LineItems.LineItem.Current().ControlMarks.Each());
        }

        private readonly DefaultConverter defaultConverter;
        private readonly DecimalConverter decimalConverter;
    }
}