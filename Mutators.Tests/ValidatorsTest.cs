using System.Collections.Generic;
using System.Linq;

using GrobExp.Compiler;
using GrobExp.Mutators;
using GrobExp.Mutators.Exceptions;
using GrobExp.Mutators.ModelConfiguration;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators.Texts;

using NUnit.Framework;

namespace Mutators.Tests
{
    public static class ValidationResultTreeNodeExtensions
    {
        public static void AssertEquivalent<T>(this ValidationResultTreeNode tree, ValidationResultTreeNode<T> other)
        {
            tree.ToArray().AssertEqualsToUsingGrobuf(other.ToArray());
        }
    }

    public class ValidatorsTest : TestBase
    {
        [Test]
        public void TestProperty()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.S).InvalidIf(data => data.A.S != null, data => null));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            validator(new TestData {A = new A {S = "zzz"}}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"S"}})}});
        }

        [Test]
        public void TestArray()
        {
            LambdaCompiler.DebugOutputDirectory = @"c:\temp";
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().Z).InvalidIf(data => data.A.B.Each().S == data.A.S, data => null));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            var o = new TestData {A = new A {S = "zzz", B = new[] {new B {S = "zzz", Z = 1}, new B {S = "qxx", Z = 2}, new B {S = "zzz", Z = 3}}}};
            var validationResultTreeNode = validator(o);
            validationResultTreeNode.AssertEquivalent(new ValidationResultTreeNode<TestData>
                {
                    {"A.B.0.Z", FormattedValidationResult.Error(null, 1, new SimplePathFormatterText {Paths = new[] {"A.B[0].Z"}})},
                    {"A.B.2.Z", FormattedValidationResult.Error(null, 3, new SimplePathFormatterText {Paths = new[] {"A.B[2].Z"}})},
                });
            o = new TestData {A = new A {S = "zzz", B = new[] {new B {S = "qzz", Z = 1}, new B {S = "qxx", Z = 2}, new B()}}};
            validator(o).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            var node = validationResultTreeNode.Traverse<TestData, int?>(x => x.A.B[0].Z);
            Assert.IsNotNull(node);
        }

        [Test]
        public void TestArraySubValidator()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().Z).InvalidIf(data => data.A.B.Each().S == "zzz", data => null));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator(data => data.A.B.Each());
            validator(new B {S = "zzz"}).AssertEquivalent(new ValidationResultTreeNode<TestData>
                {
                    {"Z", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"Z"}})}
                });
        }

        [Test]
        public void TestDoubleArray()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().C.D.Each().S).InvalidIf(data => data.A.B.Each().Z > data.A.B.Each().C.D.Each().Z, data => null));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator(data => data.A);
            var o = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B
                                        {
                                            Z = 2,
                                            C = new C
                                                {
                                                    D = new[]
                                                        {
                                                            new D {S = "zzz00", Z = 1},
                                                            new D {S = "zzz01", Z = 2},
                                                        }
                                                }
                                        },
                                    new B
                                        {
                                            Z = 2,
                                            C = new C
                                                {
                                                    D = new[]
                                                        {
                                                            new D {S = "zzz10", Z = 3},
                                                            new D {S = "zzz11", Z = 1},
                                                        }
                                                }
                                        },
                                    new B
                                        {
                                            Z = 3,
                                            C = new C
                                                {
                                                    D = new[]
                                                        {
                                                            new D {S = "zzz20", Z = 4},
                                                            new D {S = "zzz21", Z = 5},
                                                        }
                                                }
                                        }
                                }
                        }
                };
            var formattedValidationResults = validator(o.A);
            formattedValidationResults.AssertEquivalent(new ValidationResultTreeNode<A>
                {
                    {"B.0.C.D.0.S", FormattedValidationResult.Error(null, "zzz00", new SimplePathFormatterText {Paths = new[] {"B[0].C.D[0].S"}})},
                    {"B.1.C.D.1.S", FormattedValidationResult.Error(null, "zzz11", new SimplePathFormatterText {Paths = new[] {"B[1].C.D[1].S"}})},
                });
            validator(null).AssertEquivalent(new ValidationResultTreeNode<A>());
            validator(new A()).AssertEquivalent(new ValidationResultTreeNode<A>());
            validator(new A {B = new[] {new B()}}).AssertEquivalent(new ValidationResultTreeNode<A>());
            validator(new A {B = new[] {new B {C = new C()}}}).AssertEquivalent(new ValidationResultTreeNode<A>());
        }

        [Test]
        public void TestDisabledIf()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.S).InvalidIf(data => data.S == null, data => null);
                    configurator.Target(data => data.S).DisabledIf(data => data.A.S == null);
                });
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            validator(new TestData {S = "qxx", A = new A {S = "zzz"}}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            validator(new TestData {S = "qxx", A = new A {S = null}}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            validator(new TestData {S = null, A = new A {S = "zzz"}}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"S"}})}});
            validator(new TestData {S = null, A = new A {S = null}}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
        }

        [Test]
        public void TestComplexObjectIsEmpty()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.D.S).InvalidIf(data => data.D != null && data.D.S == null, data => null));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            validator(new TestData {D = new D()}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            validator(new TestData {D = new D {S = "zzz"}}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            validator(new TestData {D = new D {S = "zzz", Z = 1}}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            validator(new TestData {D = new D {Z = 1}}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"D.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"D.S"}})}});
            validator(new TestData {D = new D {E = new E {S = "zzz"}}}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"D.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"D.S"}})}});
            validator(new TestData {D = new D {E = new E {X = 10}}}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"D.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"D.S"}})}});
        }

        [Test]
        public void TestComplexObjectIsEmpty2()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.D.S).InvalidIf(data => data.D.E.Empty == null, data => null));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            validator(new TestData {D = new D()}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"D.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"D.S"}})}});
            validator(new TestData {D = new D {E = new E()}}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"D.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"D.S"}})}});
            validator(new TestData {D = new D {E = new E {Empty = new Empty()}}}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
        }

        [Test]
        public void TestExternalDependency1()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.B.Each().S).InvalidIf(data => data.A.B.Each().S == null, data => null);
                    configurator.Target(data => data.A.B.Each().S).DisabledIf(data => data.A.S == null);
                });
            Assert.Throws<FoundExternalDependencyException>(() => collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator(data => data.A.B.Each()));
        }

        [Test]
        public void TestExternalDependency2()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).InvalidIf(data => data.A.S == data.A.B.Each().S, data => null));
            Assert.Throws<FoundExternalDependencyException>(() => collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator(data => data.A.B.Each()));
        }

        [Test]
        public void TestStaticValidator()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.S).InvalidIf(data => data.S == null, data => null));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetStaticValidator(data => data.S);
            Assert.IsFalse(validator(null));
            Assert.IsFalse(validator(""));
            Assert.IsTrue(validator("zzz"));
        }

        [Test]
        public void TestValidationResultsLimit()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).InvalidIf(data => data.A.B.Each().S == null, data => null));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            var o = new TestData {A = new A {B = new B[ValidationResultTreeNode.MaxValidationResults * 2]}};
            Assert.AreEqual(ValidationResultTreeNode.MaxValidationResults, validator(o).Count());
        }

        [Test]
        public void TestValidationPriorities()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.S).InvalidIf(data => data.S.Length != 3, data => null, 3);
                    configurator.Target(data => data.S).InvalidIf(data => data.S[0] != 'z', data => null, 2);
                    configurator.Target(data => data.S).InvalidIf(data => data.S[1] != 'z', data => null, 1);
                });
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            validator(new TestData {S = "1234"}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"S", FormattedValidationResult.Error(null, "1234", new SimplePathFormatterText {Paths = new[] {"S"}}, 3)}});
            validator(new TestData {S = "123"}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"S", FormattedValidationResult.Error(null, "123", new SimplePathFormatterText {Paths = new[] {"S"}}, 2)}});
            validator(new TestData {S = "z23"}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"S", FormattedValidationResult.Error(null, "z23", new SimplePathFormatterText {Paths = new[] {"S"}}, 1)}});
            validator(new TestData {S = "zz3"}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
        }

        [Test]
        public void TestRegex1()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.S).IsLike("\\d+"));
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            validator(new TestData()).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            validator(new TestData {S = ""}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            validator(new TestData {S = "123"}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
            ValidationResultTreeNode validationResultTreeNode = validator(new TestData {S = "z123"});
            validationResultTreeNode.AssertEquivalent(new ValidationResultTreeNode<TestData> {{"S", FormattedValidationResult.Error(new ValueShouldMatchPatternText {Pattern = "\\d+", Path = new SimplePathFormatterText {Paths = new[] {"S"}}, Value = "z123"}, "z123", new SimplePathFormatterText {Paths = new[] {"S"}})}});
            validator(new TestData {S = "123x"}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"S", FormattedValidationResult.Error(new ValueShouldMatchPatternText {Pattern = "\\d+", Path = new SimplePathFormatterText {Paths = new[] {"S"}}, Value = "123x"}, "123x", new SimplePathFormatterText {Paths = new[] {"S"}})}});
        }

        [Test]
        public void TestIndexer()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.Dict["Zzz"]).Required());
            var validator = collection.GetMutatorsTree(MutatorsContext.Empty).GetValidator();
            validator(new TestData {Dict = new Dictionary<string, string> {{"Zzz", null}}}).AssertEquivalent(new ValidationResultTreeNode<TestData> {{"Dict.Zzz", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"Dict.Zzz"}})}});
            validator(new TestData {Dict = new Dictionary<string, string> {{"Zzz", "qxx"}}}).AssertEquivalent(new ValidationResultTreeNode<TestData>());
        }

        [Test]
        public void TestConvertedStaticValidator()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.S).Set(data2 => data2.T.S));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.S).InvalidIf(data => data.S == null, data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetStaticValidator(data => data.T.S);
            Assert.IsFalse(validator(null));
            Assert.IsFalse(validator(""));
            Assert.IsTrue(validator("zzz"));
        }

        [Test]
        public void TestConvertedValidatorsSimple1()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.S).Set(data2 => data2.S));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.S).InvalidIf(data => data.S == "zzz", data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {S = "qxx"}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {S = "zzz"}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"S"}}, 0)}});
        }

        [Test]
        public void TestConvertedValidatorsSimple2()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.Z).Set(data2 => data2.T.Z);
                    configurator.Target(data => data.Z).Set(data2 => data2.Z);
                });
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.Z).InvalidIf(data => data.Z >= data.A.Z, data => new ValueMustBeLessThanText {Threshold = data.A.Z}));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {Z = 1}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {Z = 10, T = new T {Z = 5}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"Z", FormattedValidationResult.Error(new ValueMustBeLessThanText {Threshold = 5}, 10, new SimplePathFormatterText {Paths = new[] {"Z"}}, 0)}});
        }

        [Test]
        public void TestConvertedValidatorsSimple3()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.Z).Set(data2 => data2.T.Z);
                    configurator.Target(data => data.Z).Set(data2 => data2.Z);
                });
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.Z).InvalidIf(data => data.Z >= data.A.Z, data => new ValueMustBeLessThanText {Threshold = data.A.Z});
                    configurator.Target(data => data.A.Z).InvalidIf(data => data.Z >= data.A.Z, data => new ValueMustBeGreaterThanText {Threshold = data.Z});
                });
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {Z = 1}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {Z = 10, T = new T {Z = 5}}).AssertEquivalent(
                new ValidationResultTreeNode<TestData2>
                    {
                        {"T.Z", FormattedValidationResult.Error(new ValueMustBeGreaterThanText {Threshold = 10}, 5, new SimplePathFormatterText {Paths = new[] {"T.Z"}}, 0)},
                        {"Z", FormattedValidationResult.Error(new ValueMustBeLessThanText {Threshold = 5}, 10, new SimplePathFormatterText {Paths = new[] {"Z"}}, 0)},
                    });
        }

        [Test]
        public void TestConvertedValidatorsSimple4()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.X).Set(data2 => data2.X));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.S).InvalidIf(data => data.S == "zzz", data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {S = "qxx"}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {S = "zzz"}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
        }

        [Test]
        public void TestValidatorFromConverter()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection,
                                                                                       configurator => configurator.Target(data => data.X).Set(data2 => data2.S, s => int.Parse(s), s => s == null || s.Length > 9 || s.Any(c => !char.IsDigit(c)) ? new ValidationResult(ValidationResultType.Error, null) : ValidationResult.Ok));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();
            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"S"}}, 2 * TreeValidatorBuilder.PriorityShift)}});
            validator(new TestData2 {S = "zzz"}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"S"}}, 2 * TreeValidatorBuilder.PriorityShift)}});
            validator(new TestData2 {S = "123456789123456789"}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"S", FormattedValidationResult.Error(null, "123456789123456789", new SimplePathFormatterText {Paths = new[] {"S"}}, 2 * TreeValidatorBuilder.PriorityShift)}});
            validator(new TestData2 {S = "123456789"}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
        }

        [Test]
        public void TestConvertedValidatorsMultipleConditionalSetters()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.S).If(data => data.X >= 0).Set(data2 => data2.S);
                    configurator.Target(data => data.S).If(data => data.X < 0).Set(data2 => data2.T.S);
                });
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection,
                                                                                                configurator => configurator.Target(data => data.S).InvalidIf(data => data.S == null, data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();
            validator(new TestData2 {X = 1}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"S"}}, 0)}});
            validator(new TestData2 {X = 1, S = "zzz"}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {X = -1}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.S"}}, 0)}});
            validator(new TestData2 {X = -1, T = new T()}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.S"}}, 0)}});
            validator(new TestData2 {X = -1, T = new T {S = "zzz"}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
        }

        [Test]
        public void TestConvertedValidatorsComplexNode()
        {
            var dataConfigurationCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollectionFromTestData2ToTestData = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.X).Set(data2 => data2.X + data2.Y));
            var converterCollectionFromTestData3ToTestData2 = new TestConverterCollection<TestData3, TestData2>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data2 => data2.X).Set(data3 => data3.X);
                    configurator.Target(data2 => data2.Y).Set(data3 => data3.Y);
                });
            var testDataConfiguratoCollection = new TestDataConfiguratorCollection<TestData>(dataConfigurationCollectionFactory, converterCollectionFactory, pathFormatterCollection,
                                                                                             configurator => configurator.Target(data => data.X).InvalidIf(data => data.X > 100, data => null));
            var testData2ConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfigurationCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            var testData3ConfiguratorCollection = new TestDataConfiguratorCollection<TestData3>(dataConfigurationCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfigurationCollectionFactory.Register(testDataConfiguratoCollection);
            dataConfigurationCollectionFactory.Register(testData2ConfiguratorCollection);
            dataConfigurationCollectionFactory.Register(testData3ConfiguratorCollection);
            converterCollectionFactory.Register(converterCollectionFromTestData2ToTestData);
            converterCollectionFactory.Register(converterCollectionFromTestData3ToTestData2);

            var validator = testData3ConfiguratorCollection.GetMutatorsTree(new[] {typeof(TestData), typeof(TestData2)}, new[] {MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty}, new[] {MutatorsContext.Empty, MutatorsContext.Empty,}).GetValidator();
            validator(new TestData3()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            var validationResultTreeNode = validator(new TestData3 {X = 50, Y = 60});
            validationResultTreeNode.AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"", FormattedValidationResult.Error(null, 110, new SimplePathFormatterText {Paths = new[] {"X", "Y"}}, 0)}});
        }

        [Test]
        public void TestConvertedValidatorsArray1()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().U.S));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).InvalidIf(data => data.A.B.Each().S == "zzz", data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {S = "qxx"}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {S = "zzz"}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.R.0.U.S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"T.R[0].U.S"}}, 0)}});
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {S = "zzz"}}, new R {U = new U {S = "qxx"}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>
                {
                    {"T.R.0.U.S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"T.R[0].U.S"}}, 0)},
                });
        }

        [Test]
        public void TestConvertedValidatorsArray_SelectMany()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection,
                                                                                       configurator => configurator.Target(data => data.A.B.Each().S)
                                                                                                                   .Set(data2 => data2.T.R.SelectMany(r => r.U.ArrayV).Each().S));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection,
                                                                                                configurator => configurator.Target(data => data.A.B.Each().S)
                                                                                                                            .InvalidIf(data => data.A.B.Each().S == "zzz",
                                                                                                                                       data => new LengthNotExactlyEqualsText {Exactly = data.A.B.Current().CurrentIndex()}));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection,
                                                                                               configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

//            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
//            validator(new TestData2 {T = new T {R = new[] {new R {U = new U ()}}}})
//                .AssertEquivalent(new ValidationResultTreeNode<TestData2>());
//            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {ArrayV = new[] {new V(), }}}}}})
//                .AssertEquivalent(new ValidationResultTreeNode<TestData2>());
//            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {ArrayV = new[] {new V{S = "qxx"}, }}}}}})
//                .AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            var path = new SimplePathFormatterText {Paths = new[] {"T.R[0].U.ArrayV[0].S"}};
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {ArrayV = new[] {new V {S = "zzz"},}}}}}})
                .AssertEquivalent(new ValidationResultTreeNode<TestData2>
                    {
                        {
                            "T.R.0.U.ArrayV.0.S", FormattedValidationResult.Error(
                                new LengthNotExactlyEqualsText {Exactly = 0, Path = path, Value = "zzz"}, "zzz", path, 0)
                        }
                    });
            path = new SimplePathFormatterText {Paths = new[] {"T.R[0].U.ArrayV[1].S"}};
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {ArrayV = new[] {new V {S = "qxx"}, new V {S = "zzz"},}}}}}})
                .AssertEquivalent(new ValidationResultTreeNode<TestData2>
                    {
                        {
                            "T.R.0.U.ArrayV.1.S", FormattedValidationResult.Error(
                                new LengthNotExactlyEqualsText {Exactly = 1, Path = path, Value = "zzz"}, "zzz", path, 0)
                        }
                    });
            path = new SimplePathFormatterText {Paths = new[] {"T.R[1].U.ArrayV[0].S"}};
            validator(new TestData2
                    {
                        T = new T
                            {
                                R = new[]
                                    {
                                        new R {U = new U {ArrayV = new[] {new V {S = "qxx"}, new V {S = "qzz"},}}},
                                        new R {U = new U {ArrayV = new[] {new V {S = "zzz"},}}}
                                    }
                            }
                    })
                .AssertEquivalent(new ValidationResultTreeNode<TestData2>
                    {
                        {
                            "T.R.1.U.ArrayV.0.S", FormattedValidationResult.Error(
                                new LengthNotExactlyEqualsText {Exactly = 2, Path = path, Value = "zzz"}, "zzz", path, 0)
                        }
                    });
        }

        [Test]
        public void TestConvertedValidatorsValidationOfArray()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().Arr.Each()).Set(data2 => data2.T.R.Each().U.Arr.Each()));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().Arr).InvalidIf(data => data.A.B.Each().Arr == null, data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {Arr = new[] {"zzz"}}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {Arr = new[] {""}}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>
                {
                    {"T.R.0.U.Arr", FormattedValidationResult.Error(null, new[] {""}, new SimplePathFormatterText {Paths = new[] {"T.R[0].U.Arr"}}, 0)}
                });
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {Arr = null}}, new R {U = new U {Arr = new[] {"", "qxx"}}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>
                {
                    {"T.R.0.U.Arr", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.R[0].U.Arr"}}, 0)},
                });
        }

        [Test]
        public void TestConvertedValidatorsAny()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.B.Each().Arr.Each()).Set(data2 => data2.T.R.Each().U.Arr.Each());
                    configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().U.S);
                });
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator =>
                                                                                                    configurator.Target(data => data.A.B.Each().S).InvalidIf(data => data.A.B.Each().Arr.Any(s => s != null && s != "zzz"), data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            var validationResultTreeNode = validator(new TestData2 {T = new T {R = new[] {new R {U = new U {Arr = new[] {"zzz"}}}}}});
            validationResultTreeNode.AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {Arr = new[] {"qxx"}}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.R.0.U.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.R[0].U.S"}}, 0)}});
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {Arr = new[] {"", "qxx"}}}, new R {U = new U {Arr = new[] {""}}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>
                {
                    {"T.R.0.U.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.R[0].U.S"}}, 0)},
                });
        }

        [Test]
        public void TestConvertedValidatorsArrayWithFilter()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Where(r => r.S == "qzz").Each().U.S));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).InvalidIf(data => data.A.B.Each().S == "zzz", data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {S = "qxx"}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {S = "qzz", U = new U {S = "qxx"}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {S = "zzz"}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {S = "qzz", U = new U {S = "zzz"}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.R.0.U.S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"T.R[0].U.S"}}, 0)}});
            validator(new TestData2
                {
                    T = new T
                        {
                            R = new[]
                                {
                                    new R {U = new U {S = "zzz"}},
                                    new R {S = "qzz", U = new U {S = "qxx"}},
                                    new R {S = "qzz", U = new U {S = "zzz"}},
                                    new R {U = new U {S = "qxx"}}
                                }
                        }
                }).AssertEquivalent(new ValidationResultTreeNode<TestData2>
                {
                    {"T.R.2.U.S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"T.R[2].U.S"}}, 0)},
                });
        }

        [Test]
        public void TestConvertedValidatorsArray2()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.B[0].S).Set(data2 => data2.T.S);
                    configurator.Target(data => data.A.B[1].S).Set(data2 => data2.S);
                });
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).InvalidIf(data => data.A.B.Each().S == "zzz", data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {S = "qxx"}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {S = "zzz"}).AssertEquivalent(new ValidationResultTreeNode<TestData2>
                {
                    {"S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"S"}}, 0)},
                });
            validator(new TestData2 {S = "zzz", T = new T {S = "qxx"}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>
                {
                    {"S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"S"}}, 0)},
                });
            validator(new TestData2 {S = "zzz", T = new T {S = "zzz"}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>
                {
                    {"T.S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"T.S"}}, 0)},
                    {"S", FormattedValidationResult.Error(null, "zzz", new SimplePathFormatterText {Paths = new[] {"S"}}, 0)},
                });
        }

        [Test]
        public void TestConvertedValidatorsSelect()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().U.S));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).InvalidIf(data => BuildCounts(data.A.B.Select(b => b.S).ToArray())[data.A.B.Each().CurrentIndex()] > 1, data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2()).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {S = "qxx"}}, new R {U = new U {S = "zzz"}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {T = new T {R = new[] {new R {U = new U {S = "qxx"}}, new R {U = new U {S = "qxx"}}}}}).AssertEquivalent(
                new ValidationResultTreeNode<TestData2>
                    {
                        {"T.R.0.U.S", FormattedValidationResult.Error(null, "qxx", new SimplePathFormatterText {Paths = new[] {"T.R[0].U.S"}}, 0)},
                        {"T.R.1.U.S", FormattedValidationResult.Error(null, "qxx", new SimplePathFormatterText {Paths = new[] {"T.R[1].U.S"}}, 0)},
                    });
        }

        [Test]
        public void TestConvertedValidatorComplexObjectIsEmpty()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.D.S).Set(data2 => data2.W.S);
                    configurator.Target(data => data.D.Z).Set(data2 => data2.W.Z);
                    configurator.Target(data => data.D.E.S).Set(data2 => data2.W.Y.S);
                    configurator.Target(data => data.D.E.X).Set(data2 => data2.W.Y.X);
                });
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.D.S).InvalidIf(data => data.D != null && data.D.S == null, data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2 {W = new W()}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {W = new W {S = "zzz"}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {W = new W {S = "zzz", Z = 1}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
            validator(new TestData2 {W = new W {Z = 1}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"W.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"W.S"}}, 0)}});
            validator(new TestData2 {W = new W {Y = new Y {S = "zzz"}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"W.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"W.S"}}, 0)}});
            validator(new TestData2 {W = new W {Y = new Y {X = 10}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"W.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"W.S"}}, 0)}});
        }

        [Test]
        public void TestConvertedValidatorWithComplexObjectConvertation()
        {
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var converterCollection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().A).Set(data2 => data2.T.R.Each().A));
            var sourceDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().A.B.C.S).InvalidIf(data => data.A.B.Each().A.B.C.S == null, data => null));
            var destDataConfiguratorCollection = new TestDataConfiguratorCollection<TestData2>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            dataConfiguratorCollectionFactory.Register(sourceDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(destDataConfiguratorCollection);
            converterCollectionFactory.Register(converterCollection);

            var validator = destDataConfiguratorCollection.GetMutatorsTree<TestData, TestData2>(MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty).GetValidator();

            validator(new TestData2 {T = new T {R = new[] {new R()}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.R.0.A.B.C.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.R[0].A.B.C.S"}}, 0)}});
            validator(new TestData2 {T = new T {R = new[] {new R {A = new CommonClassA()}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.R.0.A.B.C.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.R[0].A.B.C.S"}}, 0)}});
            validator(new TestData2 {T = new T {R = new[] {new R {A = new CommonClassA {B = new CommonClassB()}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.R.0.A.B.C.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.R[0].A.B.C.S"}}, 0)}});
            validator(new TestData2 {T = new T {R = new[] {new R {A = new CommonClassA {B = new CommonClassB {C = new CommonClassC()}}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2> {{"T.R.0.A.B.C.S", FormattedValidationResult.Error(null, null, new SimplePathFormatterText {Paths = new[] {"T.R[0].A.B.C.S"}}, 0)}});
            validator(new TestData2 {T = new T {R = new[] {new R {A = new CommonClassA {B = new CommonClassB {C = new CommonClassC {S = "zzz"}}}}}}}).AssertEquivalent(new ValidationResultTreeNode<TestData2>());
        }

        [Test]
        public void TestConvertedValidatorsConvertWithFilter()
        {
            var webDataToInnerDataConverterCollection = new TestConverterCollection<WebData, InnerData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.InnerItems.Each().InnerS).Set(data => data.WebItems.Where(item => item.WebIsRemoved == false).Current().WebS);
                    configurator.Target(data => data.InnerItems.Each().InnerItemz.Each().InnerZ).Set(data => data.WebItems.Where(item => item.WebIsRemoved == false).Current().WebItemz.Where(item => item.WebIzRemoved == false).Current().WebZ);
                });
            var modelDataToWebDataConverterCollection = new TestConverterCollection<ModelData, WebData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.WebItems.Each().WebS).Set(data => data.ModelItems.Current().ModelS);
                    configurator.Target(data => data.WebItems.Each().WebIsRemoved).Set(data => data.ModelItems.Current().ModelX > data.ModelItems.Current().ModelY);
                    configurator.Target(data => data.WebItems.Each().WebItemz.Each().WebZ).Set(data => data.ModelItems.Current().ModelItemz.Current().ModelZ);
                    configurator.Target(data => data.WebItems.Each().WebItemz.Each().WebIzRemoved).Set(data => data.ModelItems.Current().ModelItemz.Current().ModelA > data.ModelItems.Current().ModelItemz.Current().ModelB);
                });
            var dataConfiguratorCollectionFactory = new TestDataConfiguratorCollectionFactory();
            var converterCollectionFactory = new TestConverterCollectionFactory();
            var innerDataConfiguratorCollection = new TestDataConfiguratorCollection<InnerData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { configurator.Target(data => data.InnerItems.Each().InnerItemz.Each().InnerZ).Required(); });
            var webDataConfiguratorCollection = new TestDataConfiguratorCollection<WebData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            var modelDataConfiguratorCollection = new TestDataConfiguratorCollection<ModelData>(dataConfiguratorCollectionFactory, converterCollectionFactory, pathFormatterCollection, configurator => { });
            converterCollectionFactory.Register(modelDataToWebDataConverterCollection);
            converterCollectionFactory.Register(webDataToInnerDataConverterCollection);
            dataConfiguratorCollectionFactory.Register(innerDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(modelDataConfiguratorCollection);
            dataConfiguratorCollectionFactory.Register(webDataConfiguratorCollection);

            var validator = modelDataConfiguratorCollection.GetMutatorsTree(new[] {typeof(InnerData), typeof(WebData)}, new[] {MutatorsContext.Empty, MutatorsContext.Empty, MutatorsContext.Empty,}, new[] {MutatorsContext.Empty, MutatorsContext.Empty,}).GetValidator();

            var validationResultTreeNode = validator(new ModelData
                {
                    ModelItems = new[]
                        {
                            new ModelData1stLevel
                                {
                                    ModelX = 1,
                                    ModelY = 2,
                                    ModelItemz = new[]
                                        {
                                            new ModelData2ndLevel
                                                {
                                                    ModelA = 2,
                                                    ModelB = 1
                                                },
                                            new ModelData2ndLevel
                                                {
                                                    ModelA = 1,
                                                    ModelB = 2
                                                },
                                            new ModelData2ndLevel
                                                {
                                                    ModelA = 2,
                                                    ModelB = 1,
                                                    ModelZ = "zzz"
                                                },
                                        }
                                },
                            new ModelData1stLevel
                                {
                                    ModelX = 2,
                                    ModelY = 1,
                                    ModelItemz = new[]
                                        {
                                            new ModelData2ndLevel
                                                {
                                                    ModelA = 1,
                                                    ModelB = 2
                                                },
                                            new ModelData2ndLevel
                                                {
                                                    ModelA = 2,
                                                    ModelB = 1
                                                },
                                            new ModelData2ndLevel
                                                {
                                                    ModelA = 1,
                                                    ModelB = 2,
                                                    ModelZ = "zzz"
                                                },
                                        }
                                },
                        }
                });
            validationResultTreeNode.AssertEquivalent(new ValidationResultTreeNode<ModelData> {{"ModelItems.0.ModelItemz.1.ModelZ", FormattedValidationResult.Error(new ValueRequiredText(), null, new SimplePathFormatterText {Paths = new[] {"ModelItems[0].ModelItemz[1].ModelZ"}})}});
        }

        protected override void SetUp()
        {
            base.SetUp();
            pathFormatterCollection = new PathFormatterCollection();
        }

        private static int[] BuildCounts(string[] keys)
        {
            if (keys == null)
                return null;
            var counts = new Dictionary<string, int>();
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;
                if (counts.ContainsKey(key))
                    counts[key] = counts[key] + 1;
                else
                    counts[key] = 1;
            }

            return keys.Select(key => counts[key]).ToArray();
        }

        private IPathFormatterCollection pathFormatterCollection;

        [MultiLanguageTextType("ValueMustBeLessThanText")]
        public class ValueMustBeLessThanText : MultiLanguageTextBase
        {
            public object Threshold { get; set; }

            protected override void Register()
            {
                Register("RU", () => "Поле должно быть меньше " + Threshold);
                Register("EN", () => "The field must be less than " + Threshold);
            }
        }

        [MultiLanguageTextType("ValueMustBeGreaterThanText")]
        public class ValueMustBeGreaterThanText : MultiLanguageTextBase
        {
            public object Threshold { get; set; }

            protected override void Register()
            {
                Register("RU", () => "Поле должно быть больше " + Threshold);
                Register("EN", () => "The field must be greater than " + Threshold);
            }
        }

        public class InnerData2ndLevel
        {
            public string InnerZ { get; set; }
        }

        public class InnerData1stLevel
        {
            public string InnerS { get; set; }
            public InnerData2ndLevel[] InnerItemz { get; set; }
        }

        public class InnerData
        {
            public InnerData1stLevel[] InnerItems { get; set; }
        }

        public class WebData2ndLevel
        {
            public bool WebIzRemoved { get; set; }
            public string WebZ { get; set; }
        }

        public class WebData1stLevel
        {
            public bool WebIsRemoved { get; set; }
            public string WebS { get; set; }
            public WebData2ndLevel[] WebItemz { get; set; }
        }

        public class WebData
        {
            public WebData1stLevel[] WebItems { get; set; }
        }

        public class ModelData2ndLevel
        {
            public int ModelA { get; set; }
            public int ModelB { get; set; }
            public string ModelZ { get; set; }
        }

        public class ModelData1stLevel
        {
            public int ModelX { get; set; }
            public int ModelY { get; set; }
            public string ModelS { get; set; }
            public ModelData2ndLevel[] ModelItemz { get; set; }
        }

        public class ModelData
        {
            public ModelData1stLevel[] ModelItems { get; set; }
        }

        public class TestMutatorsContext : MutatorsContext
        {
            public override string GetKey()
            {
                return Key;
            }

            public string Key { get; set; }
        }

        public class CommonClassA
        {
            public CommonClassB B { get; set; }
            public string S { get; set; }
        }

        public class CommonClassB
        {
            public CommonClassC C { get; set; }
            public string S { get; set; }
        }

        public class CommonClassC
        {
            public string S { get; set; }
        }

        public class TestData
        {
            public string S { get; set; }
            public string F { get; set; }

            public A A { get; set; }

            public D D { get; set; }

            public int X { get; set; }
            public int Y { get; set; }
            public int? Z { get; set; }
            public int Q { get; set; }

            public Dictionary<string, string> Dict { get; set; }
        }

        public class A
        {
            public B[] B { get; set; }
            public int? Z { get; set; }
            public string S;
        }

        public class B
        {
            public CommonClassA A { get; set; }
            public string S { get; set; }
            public int? Z { get; set; }
            public int X { get; set; }
            public string[] Arr { get; set; }
            public C C { get; set; }
        }

        public class C
        {
            public string S { get; set; }
            public int? Z { get; set; }
            public D[] D { get; set; }
        }

        public class D
        {
            public string S { get; set; }
            public int? Z { get; set; }
            public E E { get; set; }
        }

        public class E
        {
            public string S { get; set; }
            public int X { get; set; }
            public Empty Empty { get; set; }
        }

        public class Empty
        {
        }

        public class TestData2
        {
            public string S { get; set; }

            public T T { get; set; }

            public W W { get; set; }

            public int X { get; set; }
            public int Y { get; set; }
            public int? Z { get; set; }
            public int Q { get; set; }
        }

        public class T
        {
            public R[] R { get; set; }
            public int? Z { get; set; }
            public string S { get; set; }
        }

        public class R
        {
            public CommonClassA A { get; set; }
            public U U { get; set; }
            public string S { get; set; }
        }

        public class U
        {
            public string S { get; set; }
            public int? Z { get; set; }
            public string[] Arr { get; set; }
            public V V { get; set; }
            public V[] ArrayV { get; set; }
        }

        public class V
        {
            public string S { get; set; }
            public int? Z { get; set; }
            public X[] X { get; set; }
        }

        public class X
        {
            public W W { get; set; }
            public string S { get; set; }
        }

        public class W
        {
            public string S { get; set; }
            public int? Z { get; set; }
            public Y Y { get; set; }
        }

        public class Y
        {
            public string S { get; set; }
            public int X { get; set; }
        }

        public class TestData3
        {
            public string S { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}