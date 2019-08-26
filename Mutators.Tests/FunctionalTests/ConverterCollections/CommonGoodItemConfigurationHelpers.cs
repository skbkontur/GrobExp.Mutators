using System.Linq;

using GrobExp.Mutators;
using GrobExp.Mutators.Validators.Texts;

using Mutators.Tests.FunctionalTests.InnerContract;
using Mutators.Tests.FunctionalTests.SecondOuterContract;
using Mutators.Tests.FunctionalTests.SimpleConverters;

namespace Mutators.Tests.FunctionalTests.ConverterCollections
{
    public static class CommonGoodItemConfigurationHelpers
    {
        public static void ConfigureCommonGoodItemInfo<TSecondContract, TSecondContractGoodItem, TData, TGoodItem>(
            this ConverterConfigurator<TSecondContract, TSecondContractGoodItem, TData, TGoodItem, TGoodItem> configurator)
            where TSecondContractGoodItem : ISecondContractGoodItemWithAdditionalInfo
            where TGoodItem : GoodItemWithAdditionalInfo
        {
            ConfigureGoodItemBaseInfo(configurator);

            var typeOfUnitConfigurator = configurator.GoTo(goodItem => goodItem, lin => lin.ItemDescription.Where(id => id.DescriptionFormatCode == "C"));
            typeOfUnitConfigurator.Target(goodItem => goodItem.IsReturnableContainer)
                                  .Set(x => x.FirstOrDefault(code => code.ItemDescriptionGroup.ItemDescriptionCode == "RC").ItemDescriptionGroup.ItemDescriptionCode,
                                       rc => rc == "RC");
            typeOfUnitConfigurator.Target(goodItem => goodItem.TypeOfUnit)
                                  .Set(x => x.FirstOrDefault().ItemDescriptionGroup.ItemDescriptionCode,
                                       type => defaultConverter.Convert(type));

            configurator.Target(goodItem => goodItem.Name).Set(goodItem => (from description in goodItem.ItemDescription
                                                                            where description.ItemCharacteristic.ItemCharacteristicCode == null
                                                                            where description.ItemDescriptionGroup.ItemDescription.Length > 0
                                                                            select description.ItemDescriptionGroup.ItemDescription).FirstOrDefault(), strings => ArrayStringConverter.ToString(strings));
        }

        public static void ConfigureGoodItemBaseInfo<TSecondContract, TSecondContractGoodItem, TData, TGoodItem>(
            this ConverterConfigurator<TSecondContract, TSecondContractGoodItem, TData, TGoodItem, TGoodItem> configurator)
            where TSecondContractGoodItem : ISecondContractGoodItemWithAdditionalInfo
            where TGoodItem : GoodItemBase
        {
            configurator.Target(goodItem => goodItem.GTIN).Set(sg28 => sg28.LineItem.ItemNumberIdentification.ItemIdentifier);

            configurator.Target(goodItem => goodItem.AdditionalId)
                        .Set(sg28 => (from id in sg28.AdditionalProductId
                                      from identification in id.ItemNumberIdentification
                                      where (id.ProductIdentifierCodeQualifier == "1" || id.ProductIdentifierCodeQualifier == "5")
                                            && identification.ItemTypeIdentificationCode == "STB"
                                      select identification.ItemIdentifier).FirstOrDefault());
        }

        public static void ConfigureCountiesOfOrigin<TSecondContract, TGoodItem, TData>(this ConverterConfigurator<TSecondContract, TGoodItem, TData, CommonGoodItem, CommonGoodItem> configurator)
            where TGoodItem : ISecondContractGoodItemWithAdditionalInfo
        {
            configurator.Target(goodItem => goodItem.CountriesOfOriginCode.Each())
                        .Set(sg28 => sg28.AdditionalInformation.Current().CountryOfOriginNameCode,
                             s => defaultConverter.ConvertWithDefault(s, "DefaultCountry"),
                             s => s == null,
                             s => new ValueMustBelongToText
                                 {
                                     Value = s,
                                 });
        }

        private static readonly DefaultConverter defaultConverter = new DefaultConverter();
    }
}