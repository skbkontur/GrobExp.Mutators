using System;

using GrobExp.Mutators;

namespace Mutators.Tests
{
    public class TestDataConfiguratorCollection<TData> : DataConfiguratorCollection<TData>
    {
        public TestDataConfiguratorCollection(IDataConfiguratorCollectionFactory dataConfiguratorCollectionFactory, IConverterCollectionFactory converterCollectionFactory, IPathFormatterCollection pathFormatterCollection, Action<MutatorsConfigurator<TData>> action)
            : base(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection)
        {
            this.action = action;
        }

        protected override void Configure(MutatorsContext context, MutatorsConfigurator<TData> configurator)
        {
            action(configurator);
        }

        private readonly Action<MutatorsConfigurator<TData>> action;
    }
}