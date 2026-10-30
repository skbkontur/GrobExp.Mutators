using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using GrobExp.Mutators;
using GrobExp.Mutators.CustomFields;
using GrobExp.Mutators.Validators.Texts;

using NUnit.Framework;
using NUnit.Framework.Legacy;

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
            switch (typeCode)
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
            switch (typeCode)
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
    [Parallelizable(ParallelScope.All)]
    public class TestCustomFields
    {
        [SetUp]
        public void SetUp()
        {
            converterCollectionFactory = new TestConverterCollectionFactory();
            pathFormatterCollection = new PathFormatterCollection();
            var webDataToDataConverterCollection = new TestConverterCollection<WebData, Data>(pathFormatterCollection, configurator => { configurator.Target(data => data.Items.Each().Id).Set(data => data.Items.Current().Id); });
            var modelDataToWebDataConverterCollection = new TestConverterCollection<ModelData, WebData>(pathFormatterCollection, configurator => { configurator.Target(data => data.Items.Each().Id).Set(data => data.Items.Current().Id); });
            converterCollectionFactory.Register(webDataToDataConverterCollection);
            converterCollectionFactory.Register(modelDataToWebDataConverterCollection);
        }

        [Test]
        public void TestWebDataToDataConverter()
        {
            var webDataToDataConverterCollection = new TestConverterCollection<WebData, Data>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(x => x.Items.Each().Id).Set(x => x.Items.Current().Id);
                    configurator.Target(x => x.F).Set(x => x.F);
                });
            var converter = webDataToDataConverterCollection.GetConverter(MutatorsContext.Empty);
            var data = converter(new WebData
                {
                    F = "qxx",
                    CustomFields = new Lazy<Dictionary<string, CustomFieldValue>>(() => new Dictionary<string, CustomFieldValue>
                        {
                            {"S", new CustomFieldValue {TypeCode = TypeCode.String, Value = "zzz"}},
                            {"F", new CustomFieldValue {TypeCode = TypeCode.String, Value = "zzz"}},
                            {"StrArr", new CustomFieldValue {TypeCode = TypeCode.String, Value = new[] {"zzz", "qxx"}, IsArray = true}},
                            {"Q", new CustomFieldValue {TypeCode = TypeCode.Decimal, Value = 2.0m}},
                            {"Qq", new CustomFieldValue {TypeCode = TypeCode.Double, Value = 10.0}},
                            {"E", new CustomFieldValue {TypeCode = TypeCode.String, Value = "ZZZ"}},
                            {"ComplexFieldёX", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 123}},
                            {"ComplexFieldёZёS", new CustomFieldValue {TypeCode = TypeCode.String, Value = "qzz"}},
                            {
                                "ComplexArr", new CustomFieldValue
                                    {
                                        TypeCode = TypeCode.Object,
                                        IsArray = true,
                                        TypeCodes = new Dictionary<string, TypeCode> {{"X", TypeCode.Int32}, {"ZёS", TypeCode.String}, {"ZёE", TypeCode.String}},
                                        Value = new[] {new Hashtable {{"X", 314}, {"ZёS", "qzz"}}, new Hashtable {{"X", 271}, {"ZёS", "xxx"}, {"ZёE", "QXX"}}}
                                    }
                            }
                        })
                });
            ClassicAssert.AreEqual("zzz", data.S);
            ClassicAssert.AreEqual("qxx", data.F);
            ClassicAssert.AreEqual(2.0m, data.Q);
            ClassicAssert.AreEqual(10.0, data.Qq);
            ClassicAssert.AreEqual(TestEnum.Zzz, data.E);
            ClassicAssert.IsNotNull(data.ComplexField);
            ClassicAssert.AreEqual(123, data.ComplexField.X);
            ClassicAssert.IsNotNull(data.ComplexField.Z);
            ClassicAssert.AreEqual("qzz", data.ComplexField.Z.S);
            ClassicAssert.AreEqual(TestEnum.Zzz, data.ComplexField.Z.E);
            ClassicAssert.IsNotNull(data.StrArr);
            ClassicAssert.AreEqual(2, data.StrArr.Length);
            ClassicAssert.AreEqual("zzz", data.StrArr[0]);
            ClassicAssert.AreEqual("qxx", data.StrArr[1]);
            ClassicAssert.IsNotNull(data.ComplexArr);
            ClassicAssert.AreEqual(2, data.ComplexArr.Length);
            ClassicAssert.IsNotNull(data.ComplexArr[0]);
            ClassicAssert.AreEqual(314, data.ComplexArr[0].X);
            ClassicAssert.IsNotNull(data.ComplexArr[0].Z);
            ClassicAssert.AreEqual("qzz", data.ComplexArr[0].Z.S);
            ClassicAssert.AreEqual(TestEnum.Zzz, data.ComplexArr[0].Z.E);
            ClassicAssert.IsNotNull(data.ComplexArr[1]);
            ClassicAssert.AreEqual(271, data.ComplexArr[1].X);
            ClassicAssert.IsNotNull(data.ComplexArr[1].Z);
            ClassicAssert.AreEqual("xxx", data.ComplexArr[1].Z.S);
            ClassicAssert.AreEqual(TestEnum.Qxx, data.ComplexArr[1].Z.E);
        }

        [Test]
        public void TestDataToWebDataConverter()
        {
            var dataToWebDataConverterCollection = new TestConverterCollection<Data, WebData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(x => x.Items.Each().Id).Set(x => x.Items.Current().Id);
                    configurator.Target(x => x.F).Set(x => x.F);
                });
            var converter = dataToWebDataConverterCollection.GetConverter(MutatorsContext.Empty);
            var data = converter(new Data
                {
                    S = "zzz",
                    F = "qxx",
                    E = TestEnum.Qxx,
                    StrArr = new[] {"zzz", "qxx"},
                    ComplexField = new ComplexCustomField {X = 123},
                    ComplexArr = new[] {new ComplexCustomField {X = 314, Z = new ComplexCustomFieldSubClass {S = "qzz", E = TestEnum.Qxx}}, new ComplexCustomField {X = 271, Z = new ComplexCustomFieldSubClass {S = "xxx"}}}
                });
            ClassicAssert.IsNotNull(data.CustomFields);
            ClassicAssert.IsFalse(data.CustomFields.Value.ContainsKey("F"));
            ClassicAssert.AreEqual("qxx", data.F);
            Assert.That(data.CustomFields.Value.ContainsKey("S"));
            ClassicAssert.IsNotNull(data.CustomFields.Value["S"]);
            ClassicAssert.AreEqual("zzz", data.CustomFields.Value["S"].Value);
            ClassicAssert.AreEqual(TypeCode.String, data.CustomFields.Value["S"].TypeCode);
            Assert.That(data.CustomFields.Value.ContainsKey("E"));
            ClassicAssert.IsNotNull(data.CustomFields.Value["E"]);
            ClassicAssert.AreEqual("QXX", data.CustomFields.Value["E"].Value);
            ClassicAssert.AreEqual(TypeCode.String, data.CustomFields.Value["E"].TypeCode);
            Assert.That(data.CustomFields.Value.ContainsKey("ComplexFieldёX"));
            ClassicAssert.IsNotNull(data.CustomFields.Value["ComplexFieldёX"]);
            ClassicAssert.AreEqual(123, data.CustomFields.Value["ComplexFieldёX"].Value);
            ClassicAssert.AreEqual(TypeCode.Int32, data.CustomFields.Value["ComplexFieldёX"].TypeCode);
            Assert.That(data.CustomFields.Value.ContainsKey("ComplexFieldёZёE"));
            ClassicAssert.IsNotNull(data.CustomFields.Value["ComplexFieldёZёE"]);
            ClassicAssert.AreEqual("ZZZ", data.CustomFields.Value["ComplexFieldёZёE"].Value);
            ClassicAssert.AreEqual(TypeCode.String, data.CustomFields.Value["ComplexFieldёZёE"].TypeCode);
            Assert.That(data.CustomFields.Value.ContainsKey("StrArr"));
            ClassicAssert.AreEqual(TypeCode.String, data.CustomFields.Value["StrArr"].TypeCode);
            ClassicAssert.IsTrue(data.CustomFields.Value["StrArr"].IsArray);
            var strArr = data.CustomFields.Value["StrArr"].Value as string[];
            ClassicAssert.IsNotNull(strArr);
            ClassicAssert.AreEqual(2, strArr.Length);
            ClassicAssert.AreEqual("zzz", strArr[0]);
            ClassicAssert.AreEqual("qxx", strArr[1]);
            Assert.That(data.CustomFields.Value.ContainsKey("ComplexArr"));
            ClassicAssert.AreEqual(TypeCode.Object, data.CustomFields.Value["ComplexArr"].TypeCode);
            ClassicAssert.IsTrue(data.CustomFields.Value["ComplexArr"].IsArray);
            var typeCodes = data.CustomFields.Value["ComplexArr"].TypeCodes;
            ClassicAssert.IsNotNull(typeCodes);
            Assert.That(typeCodes.ContainsKey("X"));
            ClassicAssert.AreEqual(TypeCode.Int32, typeCodes["X"]);
            Assert.That(typeCodes.ContainsKey("ZёS"));
            ClassicAssert.AreEqual(TypeCode.String, typeCodes["ZёS"]);
            Assert.That(typeCodes.ContainsKey("ZёE"));
            ClassicAssert.AreEqual(TypeCode.String, typeCodes["ZёE"]);
            var complexArr = data.CustomFields.Value["ComplexArr"].Value as object[];
            ClassicAssert.IsNotNull(complexArr);
            ClassicAssert.AreEqual(2, complexArr.Length);
            var hashtable = complexArr[0] as Hashtable;
            ClassicAssert.IsNotNull(hashtable);
            ClassicAssert.AreEqual(hashtable["X"], 314);
            ClassicAssert.AreEqual(hashtable["ZёS"], "qzz");
            ClassicAssert.AreEqual(hashtable["ZёE"], "QXX");
            hashtable = complexArr[1] as Hashtable;
            ClassicAssert.IsNotNull(hashtable);
            ClassicAssert.AreEqual(hashtable["X"], 271);
            ClassicAssert.AreEqual(hashtable["ZёS"], "xxx");
            ClassicAssert.AreEqual(hashtable["ZёE"], "ZZZ");
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
            var webDataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => {});
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(webDataConfiguratorCollection);
            var webValidator = webDataConfiguratorCollection.GetMutatorsTree<Data, WebData>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();
            webValidator(new WebData
                {
                    CustomFields = new Lazy<Dictionary<string, CustomFieldValue>>(() => new Dictionary<string, CustomFieldValue>
                        {
                            {"S", new CustomFieldValue {TypeCode = TypeCode.String}},
                            {"StrArr", new CustomFieldValue {TypeCode = TypeCode.String, IsArray = true, Value = new[] {"qxx", "zzz"}}}
                        }),
                    Items = new[]
                        {
                            new WebDataItem
                                {
                                    CustomFields = new Dictionary<string, CustomFieldValue> {{"S", new CustomFieldValue {TypeCode = TypeCode.String}}},
                                },
                        }
                }).AssertEquivalent(
                new ValidationResultTreeNode<WebData>
                    {
                        {"CustomFields.Value.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"CustomFields.Value.S.Value"}}, 0)},
                        {"CustomFields.Value.StrArr.Value.1", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"CustomFields.Value.StrArr.Value[1]"}}, 0)},
                        {"Items.0.CustomFields.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"Items[0].CustomFields.S.Value"}}, 0)},
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
            var modelDataConfiguratorCollection = new TestDataConfiguratorCollection<ModelData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => {});
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => {}));
            dataConfiguratorCollectionFactory.Register(modelDataConfiguratorCollection);
            var modelValidator = modelDataConfiguratorCollection.GetMutatorsTree(new[] {typeof(Data), typeof(WebData)}, new[] {MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty,}, new[] {MutatorsContext.Empty, MutatorsContext.Empty,}).GetValidator();
            var validationResultTreeNode = modelValidator(new ModelData
                {
                    CustomFields = new Dictionary<string, CustomFieldValue>
                        {
                            {"S", new CustomFieldValue {TypeCode = TypeCode.String}},
                            {"StrArr", new CustomFieldValue {TypeCode = TypeCode.String, IsArray = true, Value = new[] {"qxx", "zzz"}}}
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
                new ValidationResultTreeNode<ModelData>
                    {
                        {"CustomFields.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"CustomFields.S.Value"}}, 0)},
                        {"CustomFields.StrArr.Value.1", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"CustomFields.StrArr.Value[1]"}}, 0)},
                        {"Items.0.CustomFields.S.Value", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"Items[0].CustomFields.S.Value"}}, 0)},
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
                                                                                              configurator.Target(data => data.Sum).Set(data => data.DecimalArr.Sum());
                                                                                              configurator.Target(data => data.ComplexArrSum).Set(data => data.ComplexArr.Sum(x => x.Y));
                                                                                          }
            );
            var webDataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => {});
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(webDataConfiguratorCollection);
            var webMutator = webDataConfiguratorCollection.GetMutatorsTree<Data, WebData>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetTreeMutator();
            var webData = new WebData
                {
                    CustomFields = new Lazy<Dictionary<string, CustomFieldValue>>(() => new Dictionary<string, CustomFieldValue>
                        {
                            {"X", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 0}},
                            {"Y", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 1}},
                            {"Z", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 2}},
                            {"Sum", new CustomFieldValue {TypeCode = TypeCode.Decimal, Value = 0m}},
                            {"ComplexArrSum", new CustomFieldValue {TypeCode = TypeCode.Decimal, Value = 0m}},
                            {"DecimalArr", new CustomFieldValue {TypeCode = TypeCode.Decimal, IsArray = true, Value = new object[] {1m, 2m, 3m}}},
                            {
                                "ComplexArr", new CustomFieldValue
                                    {
                                        TypeCode = TypeCode.Object,
                                        IsArray = true,
                                        TypeCodes = new Dictionary<string, TypeCode> {{"Y", TypeCode.Decimal}},
                                        Value = new[] {new Hashtable {{"Y", 1m},}, new Hashtable {{"Y", 2m}}}
                                    }
                            }
                        }),
                    Items = new[]
                        {
                            new WebDataItem
                                {
                                    CustomFields = new Dictionary<string, CustomFieldValue>
                                        {
                                            {"X", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 0}},
                                            {"Y", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 1}},
                                            {"Z", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 2}}
                                        },
                                },
                        }
                };
            webMutator(webData);
            ClassicAssert.AreEqual(3, webData.CustomFields.Value["X"].Value);
            ClassicAssert.AreEqual(3, webData.Items[0].CustomFields["X"].Value);
            ClassicAssert.AreEqual(6m, webData.CustomFields.Value["Sum"].Value);
            ClassicAssert.AreEqual(3m, webData.CustomFields.Value["ComplexArrSum"].Value);
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
            var modelDataConfiguratorCollection = new TestDataConfiguratorCollection<ModelData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => {});
            dataConfiguratorCollectionFactory.Register(dataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => {}));
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
                                            {"X", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 0}},
                                            {"Y", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 1}},
                                            {"Z", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 2}}
                                        },
                                },
                        }
                };
            modelMutator(modelData);
            ClassicAssert.AreEqual(3, modelData.CustomFields["X"].Value);
            ClassicAssert.AreEqual(3, modelData.Items[0].CustomFields["X"].Value);
        }

        [Test]
        public void TestCustomFieldsContainerCopy()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var dataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator =>
                {
                    configurator.Target(x => x.CustomFieldsCopy.Value.Each().Key).Set(x => x.CustomFields.Value.Current().Key);
                    configurator.Target(x => x.CustomFieldsCopy.Value.Each().Value.Value).Set(x => x.CustomFields.Value.Current().Value.Value);
                    configurator.Target(x => x.CustomFieldsCopy.Value.Each().Value.TypeCode).Set(x => x.CustomFields.Value.Current().Value.TypeCode);
                    configurator.Target(x => x.CustomFieldsCopy.Value.Each().Value.Title).Set(x => x.CustomFields.Value.Current().Value.Title);
                });

            var mutator = dataConfiguratorCollection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var data = new WebData
                {
                    CustomFields = new Lazy<Dictionary<string, CustomFieldValue>>(() => new Dictionary<string, CustomFieldValue>
                        {
                            {"X", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 0}},
                            {"Y", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 1}},
                            {"Z", new CustomFieldValue {TypeCode = TypeCode.Int32, Value = 2}}
                        }),
                };
            mutator(data);
            ClassicAssert.AreEqual(0, data.CustomFieldsCopy.Value["X"].Value);
            ClassicAssert.AreEqual(1, data.CustomFieldsCopy.Value["Y"].Value);
            ClassicAssert.AreEqual(2, data.CustomFieldsCopy.Value["Z"].Value);
        }

        public enum TestEnum
        {
            [BeatifulName("ZZZ")]
            Zzz,

            [BeatifulName("QXX")]
            Qxx
        }

        private TestConverterCollectionFactory converterCollectionFactory;
        private PathFormatterCollection pathFormatterCollection;

        public class BeatifulNameAttribute : Attribute
        {
            public BeatifulNameAttribute(string name)
            {
                Name = name;
            }

            public string Name { get; private set; }
        }

        public class ComplexCustomFieldSubClass
        {
            [CustomField]
            public string S { get; set; }

            [CustomField]
            public TestEnum E { get; set; }
        }

        public class ComplexCustomField
        {
            [CustomField]
            public int X { get; set; }

            [CustomField]
            public decimal Y { get; set; }

            [CustomField]
            public ComplexCustomFieldSubClass Z { get; set; }
        }

        public class DataItem
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

        public class Data
        {
            [CustomField]
            public int X { get; set; }

            [CustomField]
            public int Y { get; set; }

            [CustomField]
            public int Z { get; set; }

            [CustomField]
            public string S { get; set; }

            [CustomField]
            public ComplexCustomField ComplexField { get; set; }

            [CustomField]
            public string[] StrArr { get; set; }

            [CustomField]
            public decimal[] DecimalArr { get; set; }

            [CustomField]
            public decimal Sum { get; set; }

            [CustomField]
            public decimal ComplexArrSum { get; set; }

            [CustomField]
            public decimal? Q { get; set; }

            [CustomField]
            public double? Qq { get; set; }

            [CustomField]
            public ComplexCustomField[] ComplexArr { get; set; }

            [CustomField]
            public string F { get; set; }

            [CustomField]
            public TestEnum E { get; set; }

            public DataItem[] Items { get; set; }
        }

        public class WebDataItem
        {
            public string Id { get; set; }

            [CustomFieldsContainer]
            public Dictionary<string, CustomFieldValue> CustomFields { get; set; }
        }

        public class WebData
        {
            [CustomFieldsContainer]
            public Lazy<Dictionary<string, CustomFieldValue>> CustomFields { get; set; }

            public WebDataItem[] Items { get; set; }
            public Lazy<Dictionary<string, CustomFieldValue>> CustomFieldsCopy { get; set; }
            public string F { get; set; }
        }

        public class ModelDataItem
        {
            public string Id { get; set; }

            [CustomFieldsContainer]
            public Dictionary<string, CustomFieldValue> CustomFields { get; set; }
        }

        public class ModelData
        {
            [CustomFieldsContainer]
            public Dictionary<string, CustomFieldValue> CustomFields { get; set; }

            public ModelDataItem[] Items { get; set; }
        }
    }
}