using System;

using GrobExp.Mutators;

using Vostok.Logging.Abstractions;

namespace Mutators.Tests.FunctionalTests
{
    public abstract class TestBaseConverterCollection<TSource, TDest> : ConverterCollection<TSource, TDest> where TDest : new()
    {
        protected TestBaseConverterCollection(IPathFormatterCollection pathFormatterCollection, IStringConverter stringConverter, ILog logger = null)
            : base(pathFormatterCollection, stringConverter, logger ?? new SilentLog())
        {
        }

        protected abstract void Configure(TestConverterContext converterContext, ConverterConfigurator<TSource, TDest> configurator);

        protected override void Configure(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator)
        {
            if (context is TestConverterContext testMutatorsContext)
                Configure(testMutatorsContext, configurator);
            else
                throw new InvalidOperationException($"{context.GetType().Name} is not supported");
        }
    }
}