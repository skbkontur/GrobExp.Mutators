using System;

using GrobExp.Mutators;

using Mutators.Tests.FunctionalTests.FirstOuterContract;
using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.ConverterCollections
{
    public class InnerContractToFirstContractConverterCollection : TestBaseConverterCollection<InnerDocument, FirstContractDocument>
    {
        public InnerContractToFirstContractConverterCollection(
            IPathFormatterCollection pathFormatterCollection,
            DefaultConverter defaultConverter,
            DecimalConverter decimalConverter)
            : base(pathFormatterCollection, new EnumStringConverter())
        {
            this.defaultConverter = defaultConverter;
            this.decimalConverter = decimalConverter;
        }

        protected override void Configure(TestConverterContext converterContext, ConverterConfigurator<InnerDocument, FirstContractDocument> configurator)
        {
            Configure(configurator);
        }

        private void Configure(ConverterConfigurator<InnerDocument, FirstContractDocument> configurator)
        {
            configurator.Target(x => x.CreationDateTime).Set(x => UtcNow());

            var headerConfigurator = configurator.GoTo(x => x.Header, x => x);
            headerConfigurator.Target(x => x.Sender).Set(x => x.FromGln);
            headerConfigurator.Target(x => x.Recipient).Set(x => x.ToGln);

            headerConfigurator.Target(x => x.IsTest).Set(x => x.IsTest ? "1" : null);
            headerConfigurator.Target(x => x.CreationDateTime).Set(x => DateTime.UtcNow);

            var subConfigurator = configurator.GoTo(x => x.Document[0], x => x);
            subConfigurator.Target(x => x.Comment).Set(x => Guid.NewGuid().ToString());
            subConfigurator.Target(x => x.MessageCodes[0]).Set(x => NewGuid().ToString());

            subConfigurator.Target(x => x.OriginOrder.Number).Set(x => x.OrdersNumber);
            subConfigurator.Target(x => x.OriginOrder.Date).Set(x => x.OrdersDate);

            subConfigurator.Target(x => x.DeliveryType).Set(x => x.DeliveryType.Value.ToString());

            subConfigurator.Target(x => x.DeliveryInfo.TransportBy).Set(x => defaultConverter.Convert(x.TransportBy));

            subConfigurator.Target(x => x.LineItems.TotalSumExcludingTaxes).Set(x => decimalConverter.ToString(x.SumTotal));
            subConfigurator.Target(x => x.Status)
                           .If(x => x.RecadvType.GetValueOrDefault(TypeOfDocument.Original) == TypeOfDocument.Canceled)
                           .Set(x => "canceled");

            subConfigurator.Target(x => x.IntervalLength).Set(x => x.IntervalLength.Value.ToString());

            PartyInfoConfigurators.ConfigureFromInnerToFirstContract(subConfigurator.GoTo(x => x.Buyer, x => x.Buyer), defaultConverter);
            PartyInfoConfigurators.ConfigureFromInnerToFirstContract(subConfigurator.GoTo(x => x.Seller, x => x.Supplier), defaultConverter);

            ConfigureGoodItems(subConfigurator.GoTo(x => x.LineItems.LineItem.Each(), x => x.GoodItems.Current()));
        }

        private void ConfigureGoodItems(ConverterConfigurator<InnerDocument, CommonGoodItem, FirstContractDocument, LineItem, LineItem> configurator)
        {
            configurator.Target(x => x.Gtin).Set(x => x.GTIN);

            configurator.Target(x => x.OrderedQuantity.Quantity).Set(x => decimalConverter.ToString(x.Quantity.Value));
            configurator.Target(x => x.OrderedQuantity.UnitOfMeasure).Set(x => defaultConverter.Convert(x.Quantity.MeasurementUnitCode));
            configurator.Target(x => x.TypeOfUnit).Set(x => x.IsReturnable ? "RET" : defaultConverter.Convert(x.TypeOfUnit));

            configurator.Target(x => x.ControlMarks.Each()).Set(x => x.Marks.Current());
            configurator.Target(x => x.Declarations.Each()).Set(x => defaultConverter.Convert(x.Declarations.Current().Number));

            ConfigureQuantityVariances(configurator.GoTo(x => x.ToBeReturnedQuantity.Each(), x => x.QuantityVariances.Current()));
        }

        private void ConfigureQuantityVariances(ConverterConfigurator<InnerDocument, QuantityVariance, FirstContractDocument, ReasonQuantity, ReasonQuantity> configurator)
        {
            configurator.Target(x => x.ReasonOfReturn).Set(x => x.Reason);
            configurator.Target(x => x.Quantity).Set(x => decimalConverter.ToString(x.QuantityValue));
        }

        private static DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        private static Guid NewGuid()
        {
            return Guid.NewGuid();
        }

        private readonly DefaultConverter defaultConverter;
        private readonly DecimalConverter decimalConverter;
    }
}