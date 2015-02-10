using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Compiler;
using GrobExp.Mutators;
using GrobExp.Mutators.CustomFields;
using GrobExp.Mutators.Validators.Texts;

using NUnit.Framework;

using System.Linq;

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

        public Type GetType(TypeCode typeCode)
        {
            switch(typeCode)
            {
            case TypeCode.Int32:
                return typeof(int);
            case TypeCode.String:
                return typeof(string);
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
            var webDataToDataConverterCollection = new TestConverterCollection<WebData, Data>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.Items.Each().Id).Set(data => data.Items.Current().Id);
                });
            var modelDataToWebDataConverterCollection = new TestConverterCollection<ModelData, WebData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.Items.Each().Id).Set(data => data.Items.Current().Id);
                });
            converterCollectionFactory.Register(webDataToDataConverterCollection);
            converterCollectionFactory.Register(modelDataToWebDataConverterCollection);
        }

        [Test]
        public void TestWebDataToDataConverter()
        {
            var webDataToDataConverterCollection = new TestConverterCollection<WebData, Data>(pathFormatterCollection, configurator => configurator.Target(x => x.Items.Each().Id).Set(x => x.Items.Current().Id));
            var converter = webDataToDataConverterCollection.GetConverter(MutatorsContext.Empty);
            var data = converter(new WebData
                {
                    CustomFields = new Dictionary<string, CustomFieldValue>
                        {
                            {"S", new CustomFieldValue {TypeCode = TypeCode.String, Value = "zzz"}},
                            {"StrArr", new CustomFieldValue{TypeCode = TypeCode.String, Value = new[] {"zzz", "qxx"}, IsArray = true}},
                            {"ComplexFieldёX", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 123}},
                            {"ComplexFieldёZёS", new CustomFieldValue {TypeCode = TypeCode.String, Value = "qzz"}},
                            {"ComplexArr", new CustomFieldValue
                                {
                                    TypeCode = TypeCode.Object,
                                    IsArray = true,
                                    TypeCodes = new Dictionary<string, TypeCode>{{"X", TypeCode.Int32}, {"ZёS", TypeCode.String}},
                                    Value = new[] {new Hashtable{{"X", 314}, {"ZёS", "qzz"}}, new Hashtable{{"X", 271}, {"ZёS", "xxx"}}}
                                }}
                        }
                });
            Assert.AreEqual("zzz", data.S);
            Assert.IsNotNull(data.ComplexField);
            Assert.AreEqual(123, data.ComplexField.X);
            Assert.IsNotNull(data.ComplexField.Z);
            Assert.AreEqual("qzz", data.ComplexField.Z.S);
            Assert.IsNotNull(data.StrArr);
            Assert.AreEqual(2, data.StrArr.Length);
            Assert.AreEqual("zzz", data.StrArr[0]);
            Assert.AreEqual("qxx", data.StrArr[1]);
            Assert.IsNotNull(data.ComplexArr);
            Assert.AreEqual(2, data.ComplexArr.Length);
            Assert.IsNotNull(data.ComplexArr[0]);
            Assert.AreEqual(314, data.ComplexArr[0].X);
            Assert.IsNotNull(data.ComplexArr[0].Z);
            Assert.AreEqual("qzz", data.ComplexArr[0].Z.S);
            Assert.IsNotNull(data.ComplexArr[1]);
            Assert.AreEqual(271, data.ComplexArr[1].X);
            Assert.IsNotNull(data.ComplexArr[1].Z);
            Assert.AreEqual("xxx", data.ComplexArr[1].Z.S);
        }

        [Test]
        public void TestDataToWebDataConverter()
        {
            var dataToWebDataConverterCollection = new TestConverterCollection<Data, WebData>(pathFormatterCollection, configurator => configurator.Target(x => x.Items.Each().Id).Set(x => x.Items.Current().Id));
            var converter = dataToWebDataConverterCollection.GetConverter(MutatorsContext.Empty);
            var data = converter(new Data
                {
                    S = "zzz",
                    StrArr = new [] {"zzz", "qxx"},
                    ComplexField = new ComplexCustomField{ X = 123},
                    ComplexArr = new[] {new ComplexCustomField{X = 314, Z = new ComplexCustomFieldSubClass{S = "qzz"}}, new ComplexCustomField{X = 271, Z = new ComplexCustomFieldSubClass{S = "xxx"}}}
                });
            Assert.IsNotNull(data.CustomFields);
            Assert.That(data.CustomFields.ContainsKey("S"));
            Assert.IsNotNull(data.CustomFields["S"]);
            Assert.AreEqual("zzz", data.CustomFields["S"].Value);
            Assert.AreEqual(TypeCode.String, data.CustomFields["S"].TypeCode);
            Assert.That(data.CustomFields.ContainsKey("ComplexFieldёX"));
            Assert.IsNotNull(data.CustomFields["ComplexFieldёX"]);
            Assert.AreEqual(123, data.CustomFields["ComplexFieldёX"].Value);
            Assert.AreEqual(TypeCode.Int32, data.CustomFields["ComplexFieldёX"].TypeCode);
            Assert.That(data.CustomFields.ContainsKey("StrArr"));
            Assert.AreEqual(TypeCode.String, data.CustomFields["StrArr"].TypeCode);
            Assert.IsTrue(data.CustomFields["StrArr"].IsArray);
            var strArr = data.CustomFields["StrArr"].Value as string[];
            Assert.IsNotNull(strArr);
            Assert.AreEqual(2, strArr.Length);
            Assert.AreEqual("zzz", strArr[0]);
            Assert.AreEqual("qxx", strArr[1]);
            Assert.That(data.CustomFields.ContainsKey("ComplexArr"));
            Assert.AreEqual(TypeCode.Object, data.CustomFields["ComplexArr"].TypeCode);
            Assert.IsTrue(data.CustomFields["ComplexArr"].IsArray);
            var typeCodes = data.CustomFields["ComplexArr"].TypeCodes;
            Assert.IsNotNull(typeCodes);
            Assert.That(typeCodes.ContainsKey("X"));
            Assert.AreEqual(TypeCode.Int32, typeCodes["X"]);
            Assert.That(typeCodes.ContainsKey("ZёS"));
            Assert.AreEqual(TypeCode.String, typeCodes["ZёS"]);
            var complexArr = data.CustomFields["ComplexArr"].Value as object[];
            Assert.IsNotNull(complexArr);
            Assert.AreEqual(2, complexArr.Length);
            var hashtable = complexArr[0] as Hashtable;
            Assert.IsNotNull(hashtable);
            Assert.AreEqual(hashtable["X"], 314);
            Assert.AreEqual(hashtable["ZёS"], "qzz");
            hashtable = complexArr[1] as Hashtable;
            Assert.IsNotNull(hashtable);
            Assert.AreEqual(hashtable["X"], 271);
            Assert.AreEqual(hashtable["ZёS"], "xxx");
        }

        [Test]
        public void TestWebDataValidator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<Data>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection,
                configurator =>
                    {
                        configurator.Target(data => data.S).Required();
                        configurator.Target(data => data.StrArr.Each()).InvalidIf(data => data.StrArr.Current() == "zzz", data => null);
                        configurator.Target(data => data.Items.Each().S).Required();
                    }
            );
            var webDataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(webDataConfiguratorCollection);
            var webValidator = webDataConfiguratorCollection.GetMutatorsTree<Data, WebData>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();
            webValidator(new WebData
                {
                    CustomFields = new Dictionary<string, CustomFieldValue>
                        {
                            {"S", new CustomFieldValue {TypeCode = TypeCode.String}},
                            {"StrArr", new CustomFieldValue {TypeCode = TypeCode.String, IsArray = true, Value = new[] {"qxx", "zzz"}}}
                        },
                    Items = new[]
                        {
                            new WebDataItem
                                {
                                    CustomFields = new Dictionary<string, CustomFieldValue> {{"S", new CustomFieldValue {TypeCode = TypeCode.String}}},
                                },
                        }
                }).AssertEquivalent(
                new ValidationResultTreeNode
                    {
                        {"CustomFields.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"CustomFields[S].Value"}}, 0)},
                        {"CustomFields.StrArr.Value.1", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"CustomFields[StrArr].Value[1]"}}, 0)},
                        {"Items.0.CustomFields.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"Items[0].CustomFields[S].Value"}}, 0)},
                    }
            );
        }

        [Test]
        public void TestModelDataValidator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<Data>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection,
                configurator =>
                    {
                        configurator.Target(data => data.S).Required();
                        configurator.Target(data => data.StrArr.Each()).InvalidIf(data => data.StrArr.Current() == "zzz", data => null);
                        configurator.Target(data => data.Items.Each().S).Required();
                    }
            );
            var modelDataConfiguratorCollection = new TestDataConfiguratorCollection<ModelData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { }));
            dataConfiguratorCollectionFactory.Register(modelDataConfiguratorCollection);
            var modelValidator = modelDataConfiguratorCollection.GetMutatorsTree(new[] {typeof(Data), typeof(WebData)}, new[] {MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty,}, new[] {MutatorsContext.Empty, MutatorsContext.Empty,}).GetValidator();
            var validationResultTreeNode = modelValidator(new ModelData
                {
                    CustomFields = new Dictionary<string, CustomFieldValue>
                        {
                            {"S", new CustomFieldValue {TypeCode = TypeCode.String}}, {"StrArr", new CustomFieldValue {TypeCode = TypeCode.String, IsArray = true, Value = new[] {"qxx", "zzz"}}}
                        },
                    Items = new[]
                        {
                            new ModelDataItem
                                {
                                    CustomFields = new Dictionary<string, CustomFieldValue> {{"S", new CustomFieldValue {TypeCode = TypeCode.String}}},
                                },
                        }
                });
            validationResultTreeNode.AssertEquivalent(
                new ValidationResultTreeNode
                    {
                        {"CustomFields.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"CustomFields[S].Value"}}, 0)},
                        {"CustomFields.StrArr.Value.1", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"CustomFields[StrArr].Value[1]"}}, 0)},
                        {"Items.0.CustomFields.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"Items[0].CustomFields[S].Value"}}, 0)},
                    }
            );
        }

        [Test]
        public void TestWebDataMutator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<Data>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection,
                configurator =>
                    {
                        configurator.Target(data => data.X).Set(data => data.Y + data.Z);
                        configurator.Target(data => data.Items.Each().X).Set(data => data.Items.Current().Y + data.Items.Current().Z);
                    }
            );
            var webDataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(webDataConfiguratorCollection);
            var webMutator = webDataConfiguratorCollection.GetMutatorsTree<Data, WebData>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetTreeMutator();
            var webData = new WebData
                {
                    CustomFields = new Dictionary<string, CustomFieldValue>
                        {
                            {"X", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 0}},
                            {"Y", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 1}},
                            {"Z", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 2}}
                        },
                    Items = new[]
                        {
                            new WebDataItem
                                {
                                    CustomFields = new Dictionary<string, CustomFieldValue>
                                        {
                                            {"X", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 0}},
                                            {"Y", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 1}},
                                            {"Z", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 2}}
                                        },
                                }, 
                        }
                };
            webMutator(webData);
            Assert.AreEqual(3, webData.CustomFields["X"].Value);
            Assert.AreEqual(3, webData.Items[0].CustomFields["X"].Value);
        }

        [Test]
        public void TestModelDataMutator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<Data>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection,
                configurator =>
                    {
                        configurator.Target(data => data.X).Set(data => data.Y + data.Z);
                        configurator.Target(data => data.Items.Each().X).Set(data => data.Items.Current().Y + data.Items.Current().Z);
                    }
            );
            var modelDataConfiguratorCollection = new TestDataConfiguratorCollection<ModelData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { }));
            dataConfiguratorCollectionFactory.Register(modelDataConfiguratorCollection);
            var modelMutator = modelDataConfiguratorCollection.GetMutatorsTree(new[] {typeof(Data), typeof(WebData)}, new[] {MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty,}, new[] {MutatorsContext.Empty, MutatorsContext.Empty,}).GetTreeMutator();
            var modelData = new ModelData
                {
                    CustomFields = new Dictionary<string, CustomFieldValue>
                        {
                            {"X", new CustomFieldValue {Value = 0, TypeCode = TypeCode.Int32}},
                            {"Y", new CustomFieldValue {Value = 1, TypeCode = TypeCode.Int32}},
                            {"Z", new CustomFieldValue {Value = 2, TypeCode = TypeCode.Int32}}
                        },
                    Items = new[]
                        {
                            new ModelDataItem
                                {
                                    CustomFields = new Dictionary<string, CustomFieldValue>
                                        {
                                            {"X", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 0}},
                                            {"Y", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 1}},
                                            {"Z", new CustomFieldValue{TypeCode = TypeCode.Int32, Value = 2}}
                                        },
                                }, 
                        }
                };
            modelMutator(modelData);
            Assert.AreEqual(3, modelData.CustomFields["X"].Value);
            Assert.AreEqual(3, modelData.Items[0].CustomFields["X"].Value);
        }

        [Test]
        public void TestCustomFieldsContainerCopy()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator =>
            {
                configurator.Target(x => x.CustomFieldsCopy.Each().Key).Set(x => x.CustomFields.Current().Key);
                configurator.Target(x => x.CustomFieldsCopy.Each().Value.Value).Set(x => x.CustomFields.Current().Value.Value);
                configurator.Target(x => x.CustomFieldsCopy.Each().Value.TypeCode).Set(x => x.CustomFields.Current().Value.TypeCode);
                configurator.Target(x => x.CustomFieldsCopy.Each().Value.Title).Set(x => x.CustomFields.Current().Value.Title);
            });

            var mutator = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var data = new WebData
            {
                CustomFields = new Dictionary<string, CustomFieldValue>
                {
                    {"X", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 0}},
                    {"Y", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 1}},
                    {"Z", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 2}}
                },
            };
            mutator(data);
            Assert.AreEqual(0, data.CustomFieldsCopy["X"].Value);
            Assert.AreEqual(1, data.CustomFieldsCopy["Y"].Value);
            Assert.AreEqual(2, data.CustomFieldsCopy["Z"].Value);
        }

        private TestConverterCollectionFactory converterCollectionFactory;
        private PathFormatterCollection pathFormatterCollection;

        private class ComplexCustomFieldSubClass
        {
            [CustomField]
            public string S { get; set; }
        }

        private class ComplexCustomField
        {
            [CustomField()]
            public int X { get; set; }

            [CustomField()]
            public ComplexCustomFieldSubClass Z { get; set; }
        }

        private class DataItem
        {
            public string Id { get; set; }

            [CustomField]
            public int X { get; set; }

            [CustomField]
            public int Y { get; set; }

            [CustomField]
            public int Z { get; set; }

            [CustomField]
            public string S { get; set; }
        }

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

            [CustomField()]
            public ComplexCustomField ComplexField { get; set; }

            [CustomField]
            public string[] StrArr { get; set; }

            [CustomField]
            public ComplexCustomField[] ComplexArr { get; set; }

            public DataItem[] Items { get; set; }
        }

        private class WebDataItem
        {
            public string Id { get; set; }

            [CustomFieldsContainer]
            public Dictionary<string, CustomFieldValue> CustomFields { get; set; }
        }

        private class WebData
        {
            [CustomFieldsContainer]
            public Dictionary<string, CustomFieldValue> CustomFields { get; set; }

            public WebDataItem[] Items { get; set; }
            public Dictionary<string, CustomFieldValue> CustomFieldsCopy { get; set; }
        }

        private class ModelDataItem
        {
            public string Id { get; set; }

            [CustomFieldsContainer]
            public Dictionary<string, CustomFieldValue> CustomFields { get; set; }
        }

        private class ModelData
        {
            [CustomFieldsContainer]
            public Dictionary<string, CustomFieldValue> CustomFields { get; set; }

            public ModelDataItem[] Items { get; set; }
        }
    }
}