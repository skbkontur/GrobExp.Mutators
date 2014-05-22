using System;

using GrobExp.Mutators;
using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests
{
    public class TestConverterCollection<TSource, TDest> : ConverterCollection<TSource, TDest> where TDest : new()
    {
        public TestConverterCollection(IPathFormatterCollection pathFormatterCollection, ICustomFieldsConverter customFieldsConverter, Action<ConverterConfigurator<TSource, TDest>> action)
            : base(pathFormatterCollection, customFieldsConverter)
        {
            this.action = action;
        }

        protected override void Configure(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator)
        {
            action(configurator);
        }

        private readonly Action<ConverterConfigurator<TSource, TDest>> action;
    }
}