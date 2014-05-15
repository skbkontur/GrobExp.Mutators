using System;
using System.Collections.Generic;

using GrobExp.Mutators;
using GrobExp.Mutators.CustomFields;
using GrobExp.Mutators.Validators.Texts;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class TestCustomFieldConverter : ICustomFieldsConverter
    {
        public string ConvertToString(object value)
        {
            return value == null ? null : value.ToString();
        }

        public object ConvertFromString(string value, TypeCode typeCode)
        {
            switch(typeCode)
            {
            case TypeCode.Int32:
                return int.Parse(value);
            case TypeCode.String:
                return value;
            default:
                throw new NotSupportedException();
            }
        }
    }

    [TestFixture]
    public class TestCustomFields
    {
        [SetUp]
        public void SetUp()
        {
            converterCollectionFactory = new TestConverterCollectionFactory();
            pathFormatterCollection = new PathFormatterCollection();
            var webDataToDataConverterCollection = new TestConverterCollection<WebData, Data>(pathFormatterCollection, configurator => { }
/*                {
                    configurator.Target(data => data.X).Set(webData => (int)webData.CustomFields["X"]);
                    configurator.Target(data => data.Y).Set(webData => (int)webData.CustomFields["Y"]);
                    configurator.Target(data => data.Z).Set(webData => (int)webData.CustomFields["Z"]);
                    configurator.Target(data => data.S).Set(webData => (string)webData.CustomFields["S"]);
                }*/);
            var modelDataToWebDataConverterCollection = new TestConverterCollection<ModelData, WebData>(pathFormatterCollection, configurator => { }
                /*{
                    configurator.Target(webData => webData.CustomFields.Each().Key).Set(modelData => modelData.CustomFields.Current().Key);
                    configurator.Target(webData => webData.CustomFields.Each().Value).Set(modelData => modelData.CustomFields.Current().Value.Value);
                }*/);
            converterCollectionFactory.Register(webDataToDataConverterCollection);
            converterCollectionFactory.Register(modelDataToWebDataConverterCollection);
        }

        [Test]
        public void TestWebDataValidator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<Data>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { configurator.Target(data => data.S).Required(); });
            var webDataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(webDataConfiguratorCollection);
            var webValidator = webDataConfiguratorCollection.GetMutatorsTree<Data, WebData>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();
            webValidator(new WebData {CustomFields = new Dictionary<string, object> {{"S", null}}}).AssertEquivalent(new ValidationResultTreeNode {{"CustomFields.S", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"CustomFields[S]"}}, 0)}});
        }

        [Test]
        public void TestModelDataValidator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<Data>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { configurator.Target(data => data.S).Required(); });
            var modelDataConfiguratorCollection = new TestDataConfiguratorCollection<ModelData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { }));
            dataConfiguratorCollectionFactory.Register(modelDataConfiguratorCollection);
            var modelValidator = modelDataConfiguratorCollection.GetMutatorsTree(new[] {typeof(Data), typeof(WebData)}, new[] {MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty,}, new[] {MutatorsContext.Empty, MutatorsContext.Empty,}).GetValidator();
            modelValidator(new ModelData {CustomFields = new CustomFieldsContainer {{"S", null}}}).AssertEquivalent(new ValidationResultTreeNode {{"CustomFields.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"CustomFields[S].Value"}}, 0)}});
        }

        [Test]
        public void TestWebDataMutator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<Data>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { configurator.Target(data => data.X).Set(data => data.Y + data.Z); });
            var webDataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(webDataConfiguratorCollection);
            var webMutator = webDataConfiguratorCollection.GetMutatorsTree<Data, WebData>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetTreeMutator();
            var webData = new WebData {CustomFields = new Dictionary<string, object> {{"X", 0}, {"Y", 1}, {"Z", 2}}};
            webMutator(webData);
            Assert.AreEqual(3, webData.CustomFields["X"]);
        }

        [Test]
        public void TestModelDataMutator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<Data>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { configurator.Target(data => data.X).Set(data => data.Y + data.Z); });
            var modelDataConfiguratorCollection = new TestDataConfiguratorCollection<ModelData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { }));
            dataConfiguratorCollectionFactory.Register(modelDataConfiguratorCollection);
            var modelMutator = modelDataConfiguratorCollection.GetMutatorsTree(new[] {typeof(Data), typeof(WebData)}, new[] {MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty,}, new[] {MutatorsContext.Empty, MutatorsContext.Empty,}).GetTreeMutator();
            var converter = new TestCustomFieldConverter();
            var modelData = new ModelData {CustomFields = new CustomFieldsContainer {{"X", new CustomFieldValue(converter, TypeCode.Int32) {Value = 0}}, {"Y", new CustomFieldValue(converter, TypeCode.Int32) {Value = 1}}, {"Z", new CustomFieldValue(converter, TypeCode.Int32) {Value = 2}}}};
            modelMutator(modelData);
            Assert.AreEqual(3, modelData.CustomFields["X"].Value);
        }

        private TestConverterCollectionFactory converterCollectionFactory;
        private PathFormatterCollection pathFormatterCollection;

        private class Data
        {
            [CustomField]
            public int X { get; set; }

            [CustomField]
            public int Y { get; set; }

            [CustomField]
            public int Z { get; set; }

            [CustomField]
            public string S { get; set; }
        }

        private class WebData
        {
            [CustomFieldsContainer]
            public Dictionary<string, object> CustomFields { get; set; }
        }

        private class ModelData
        {
            [CustomFieldsContainer]
            public CustomFieldsContainer CustomFields { get; set; }
        }
    }
}