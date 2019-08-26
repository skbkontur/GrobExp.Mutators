using System.Linq;

using GrobExp.Mutators;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public static class SecondContractMonetaryAmountsConfiguratorHelpers
    {
        public static void ConfigureMonetaryAmountsInfo<TSecondContract, TMessage, TData, T>(
            this ConverterConfigurator<TSecondContract, TMessage, TData, T, T> configurator,
            params MonetaryAmountConfig<T>[] monetaryAmountConfigs)
            where TMessage : IMonetaryAmountsArrayContainer
        {
            foreach (var monetaryAmountConfig in monetaryAmountConfigs)
            {
                var config = monetaryAmountConfig;
                foreach (var code in config.MonetaryAmountsFunctionalCodes)
                {
                    configurator.If(message => (from amount in message.MonetaryAmount
                                                where amount.MonetaryAmountGroup.MonetaryAmountTypeCodeQualifier == code
                                                select amount.MonetaryAmountGroup.MonetaryAmount).FirstOrDefault() != null)
                                .Target(config.PathsToMOA)
                                .Set(message => (from amount in message.MonetaryAmount
                                                 where amount.MonetaryAmountGroup.MonetaryAmountTypeCodeQualifier == code
                                                 select amount.MonetaryAmountGroup.MonetaryAmount).FirstOrDefault(),
                                     s => StaticPriceFormatter.Parse(s),
                                     s => new FloatingPointNumberValidator().Validate(s));
                }
            }
        }
    }
}