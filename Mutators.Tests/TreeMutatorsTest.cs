using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators;
using GrobExp.Mutators.Exceptions;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class TreeMutatorsTest : TestBase
    {
        [Test]
        public void TestProperty()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.S).NullifyIf(data => data.A.S != null));
            MutatorsTreeBase<TestData> mutatorsTree = collection.GetMutatorsTree(MutatorsContext.Empty);
            Action<TestData> mutator = mutatorsTree.GetTreeMutator();
            Console.WriteLine(mutatorsTree);
            var o = new TestData {A = new A {S = "zzz"}, S = "qxx"};
            mutator(o);
            Assert.That(o.S, Is.Null.Or.Empty);
            o = new TestData {S = "qxx"};
            mutator(o);
            Assert.AreEqual("qxx", o.S);
        }

        [Test]
        public void TestArray()
        {
            //var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().Z).NullifyIf(data => data.A.B.Each().S == data.A.S));
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.B.Each().Z).NullifyIf(data => data.A.B.Each().S == data.A.S);
/*
                    configurator.Array(data => data.A.B).PushBack().Target(b >= b.S).Set(data => data.S);
                    configurator.Array(data => data.A.B).PushRange(data => data.A.B).Target(b => b.S).Set(b => b.S);
*/
                });
            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData {A = new A {S = "zzz", B = new[] {new B {S = "zzz", Z = 1}, new B {S = "qxx", Z = 2}, new B {S = "zzz", Z = 3}}}};
            mutator(o);
            Assert.IsNull(o.A.B[0].Z);
            Assert.AreEqual(2, o.A.B[1].Z);
            Assert.IsNull(o.A.B[2].Z);
        }

        [Test]
        public void TestDoubleArray()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.B.Each().C.D.Each().S).NullifyIf(data => data.A.B.Each().Z > data.A.B.Each().C.D.Each().Z);
                    configurator.Target(data => data.A.B.Each().Z).NullifyIf(data => data.A.B.Each().Z < 0);
                });
            MutatorsTreeBase<TestData> mutatorsTree = collection.GetMutatorsTree(MutatorsContext.Empty);
            Action<TestData> mutator = mutatorsTree.GetTreeMutator();
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
            mutator(o);
            Assert.That(o.A.B[0].C.D[0].S, Is.Null.Or.Empty);
            Assert.AreEqual("zzz01", o.A.B[0].C.D[1].S);
            Assert.AreEqual("zzz10", o.A.B[1].C.D[0].S);
            Assert.That(o.A.B[1].C.D[1].S, Is.Null.Or.Empty);
            Assert.AreEqual("zzz20", o.A.B[2].C.D[0].S);
            Assert.AreEqual("zzz21", o.A.B[2].C.D[1].S);
            Action<A> subMutator = mutatorsTree.GetTreeMutator(data => data.A);
            var a = new A
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
                };
            subMutator(a);
            Assert.That(a.B[0].C.D[0].S, Is.Null.Or.Empty);
            Assert.AreEqual("zzz01", a.B[0].C.D[1].S);
            Assert.AreEqual("zzz10", a.B[1].C.D[0].S);
            Assert.That(a.B[1].C.D[1].S, Is.Null.Or.Empty);
            Assert.AreEqual("zzz20", a.B[2].C.D[0].S);
            Assert.AreEqual("zzz21", a.B[2].C.D[1].S);
        }

        [Test]
        public void TestSelfDependency()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().Z).NullifyIf(data => data.A.B.Each().Z == data.A.Z));
            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData {A = new A {Z = 1, B = new[] {new B {Z = 1}, new B {Z = 2}, new B {Z = 3}}}};
            mutator(o);
            Assert.IsNull(o.A.B[0].Z);
            Assert.AreEqual(2, o.A.B[1].Z);
            Assert.AreEqual(3, o.A.B[2].Z);
        }

        [Test]
        public void TestExternalDependency()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).NullifyIf(data => data.A.S == data.A.B.Each().S));
            Assert.Throws<FoundExternalDependencyException>(() => collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator(data => data.A.B.Each()));
        }

        [Test]
        public void TestCyclicDependency()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.Qxx.A0).NullifyIf(data => data.Qxx.A1 < data.Qxx.A2);
                    configurator.Target(data => data.Qxx.A1).NullifyIf(data => data.Qxx.A2 < data.Qxx.A3);
                    configurator.Target(data => data.Qxx.A2).NullifyIf(data => data.Qxx.A3 < data.Qxx.A0);
                });
            Assert.Throws<FoundCyclicDependencyException>(() => collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator());
        }

        [Test]
        public void TestEqualsToSimple()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.Z).Set(data => data.X + data.Y);
                    configurator.Target(data => data.Q).Set(data => data.X * data.Z - data.Y);
                });
            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData {X = 1, Y = 2, Z = 1, Q = 2};
            mutator(o);
            Assert.AreEqual(1, o.X);
            Assert.AreEqual(2, o.Y);
            Assert.AreEqual(3, o.Z);
            Assert.AreEqual(1, o.Q);
        }

        [Test]
        public void TestSimpleDependency()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.Qxx.A0).Set(data => data.A.B[0].Хрень[0].Length);
                    configurator.Target(data => data.A.B).Set(data => new[] {new B {Хрень = new[] {"GRobas"}}});
                });
            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData();
            mutator(o);
            Assert.AreEqual("GRobas".Length, o.Qxx.A0);
        }

        [Test]
        public void TestEqualsToLinq1()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().Z).Set(data => data.A.B.Each().C.D.Sum(d => d.Z)));
            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B
                                        {
                                            Z = 1,
                                            C = new C
                                                {
                                                    D = new[]
                                                        {
                                                            new D {Z = 2},
                                                            new D {Z = 3},
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
                                                            new D {Z = 3},
                                                            new D {Z = 4},
                                                            new D {Z = 5},
                                                        }
                                                }
                                        }
                                }
                        }
                };
            mutator(o);
            Assert.AreEqual(5, o.A.B[0].Z);
            Assert.AreEqual(12, o.A.B[1].Z);
            Action<B> subMutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator(data => data.A.B.Each());
            var b = new B
                {
                    Z = 1,
                    C = new C
                        {
                            D = new[]
                                {
                                    new D {Z = 2},
                                    new D {Z = 3},
                                }
                        }
                };
            subMutator(b);
            Assert.AreEqual(5, b.Z);
        }

        [Test]
        public void TestEqualsToArrayLength()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Length).Set(data => 1));
            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData();
            mutator(o);
            Assert.AreEqual(1, o.A.B.Length);
        }

        [Test]
        public void TestEqualsToArrayLengthCurrentIndex()
        {
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.X).Set(data => data.A.B.Sum(b => b.X));
                    configurator.Target(data => data.A.B.Each().X).Set(data => data.A.B.Each().CurrentIndex() + 1);
                    configurator.Target(data => data.A.B.Length).Set(data => 3);
                });
            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData();
            mutator(o);
            var expected = new TestData
                {
                    X = 6,
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {X = 1},
                                    new B {X = 2},
                                    new B {X = 3},
                                }
                        }
                };
            o.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestDynamic1()
        {
            var generator = new IdGenerator();
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.S).Set(data => generator.GetId().Dynamic()));

            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData();
            mutator(o);
            string id = o.S;
            Assert.That(id, Is.Not.Null.And.Not.Empty);
            mutator(o);
            Assert.AreNotEqual(id, o.S);
        }

        [Test]
        public void TestDynamic2()
        {
            var generator = new IdGenerator();
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data => generator.GetId().Dynamic()));

            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            var o = new TestData {A = new A {B = new[] {new B(), new B()}}};
            mutator(o);
            Assert.That(o.A.B[0].S, Is.Not.Null.And.Not.Empty);
            Assert.That(o.A.B[1].S, Is.Not.Null.And.Not.Empty);
            Assert.AreNotEqual(o.A.B[0].S, o.A.B[1].S);
        }

        [Test]
        public void TestConvertNullableDateTime()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.Date).Set(data2 => data2.Date1 ?? data2.Date2));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2 {Date1 = new DateTime(2010, 1, 1)};
            converter(from, to);
            var expected = new TestData {Date = new DateTime(2010, 1, 1)};
            to.AssertEqualsToUsingGrobuf(expected);
            to = new TestData();
            from = new TestData2 {Date1 = new DateTime(2010, 1, 1), Date2 = new DateTime(2011, 2, 2)};
            converter(from, to);
            expected = new TestData {Date = new DateTime(2010, 1, 1)};
            to.AssertEqualsToUsingGrobuf(expected);
            to = new TestData();
            from = new TestData2 {Date2 = new DateTime(2011, 2, 2)};
            converter(from, to);
            expected = new TestData {Date = new DateTime(2011, 2, 2)};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvert1()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.S).Set(data2 => data2.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2 {S = "zzz"};
            converter(from, to);
            var expected = new TestData {S = "zzz"};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test(Description = "Mutators cannot deal with different source arrays for single destination path")]
        public void TestConvertWithDifferentArraySources()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.Хрень.Each()).If(data => data.Q != 3).Set(data2 => data2.Чужь.Current());
                    configurator.Target(data => data.Хрень.Each()).If(data => data.Q == 3).Set(data2 => data2.T.R.Current().S);
                });
            var converter = collection.GetConverter(MutatorsContext.Empty);
            var exception = Assert.Throws<InvalidOperationException>(() => converter(new TestData2
                {
                    Q = 3,
                    Чужь = new[] {"abc", "def"},
                    T = new T
                        {
                            R = new[]
                                {
                                    new R
                                        {
                                            S = "first",
                                        },
                                    new R
                                        {
                                            S = "second",
                                        }
                                }
                        }
                }));
            Assert.That(exception.Message, Does.Match(@"^Method T Current\[T\].* cannot be invoked$"));
        }

        [Test]
        public void TestConvert2()
        {
            Expression<Func<TestData2, bool?>> condition = data => data.X == 1;
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.S).If(condition).Set(data2 => data2.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2 {S = "zzz"};
            converter(from, to);
            var expected = new TestData();
            to.AssertEqualsToUsingGrobuf(expected);
            to = new TestData();
            from = new TestData2 {S = "zzz", X = 1};
            converter(from, to);
            expected = new TestData {S = "zzz"};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvert3()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.S).Set(data2 => data2.T.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2 {T = new T {S = "zzz"}};
            converter(from, to);
            var expected = new TestData {S = "zzz"};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvert4()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.S).Set(data2 => data2.T.S);
                    configurator.Target(data => data.F).Set((data2, data) => data.S);
                });
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2 {T = new T {S = "zzz"}};
            converter(from, to);
            var expected = new TestData {S = "zzz", F = "zzz"};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test(Description = "Group by is not supported by DependenciesExtractor")]
        public void TestConvert_GroupByIsNotSupported()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    var subConfigurator = configurator.GoTo(x => x.A.B.Each(), x => GroupZzz(x.T.R.Where(r => r.U.S == "zzz").Select(r => r.U.V.X).FirstOrDefault()).Current());
                    subConfigurator.Target(b => b.X).Set(xs => xs.Sum(x => x.W.Z ?? 0));
                });
            Assert.Throws<NotSupportedException>(() => collection.GetMerger(MutatorsContext.Empty));
        }

        private X[][] GroupZzz(IEnumerable<X> enumerable)
        {
            return enumerable.GroupBy(x => x.S).Select(xs => xs.ToArray()).ToArray();
        }

        [Test]
        public void TestConvertIf1()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.If((data2, data) => data2.X + data.Y == 2).Target(data => data.S).Set(data2 => data2.T.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData {Y = 1};
            var from = new TestData2 {T = new T {S = "zzz"}, X = 1};
            converter(from, to);
            var expected = new TestData {S = "zzz", Y = 1};
            to.AssertEqualsToUsingGrobuf(expected);
            to = new TestData {Y = 2};
            from = new TestData2 {T = new T {S = "zzz"}, X = 1};
            converter(from, to);
            expected = new TestData {Y = 2};
            to.AssertEqualsToUsingGrobuf(expected);
            to = new TestData {Y = 2};
            from = new TestData2 {T = new T {S = "zzz"}, X = 0};
            converter(from, to);
            expected = new TestData {S = "zzz", Y = 2};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertIf2()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    ConverterConfigurator<TestData2, T, TestData, A, A> subConfigurator = configurator.GoTo(data => data.A, data2 => data2.T);
                    subConfigurator.If((data2, data) => data2.Z + data.Z == 2).Target(data => data.S).Set(data2 => data2.S);
                });
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData {A = new A {Z = 1}};
            var from = new TestData2 {T = new T {S = "zzz", Z = 1}};
            converter(from, to);
            var expected = new TestData {A = new A {S = "zzz", Z = 1}};
            to.AssertEqualsToUsingGrobuf(expected);
            to = new TestData {A = new A {Z = 2}};
            from = new TestData2 {T = new T {S = "zzz", Z = 1}};
            converter(from, to);
            expected = new TestData {A = new A {Z = 2}};
            to.AssertEqualsToUsingGrobuf(expected);
            to = new TestData {A = new A {Z = 2}};
            from = new TestData2 {T = new T {S = "zzz", Z = 0}};
            converter(from, to);
            expected = new TestData {A = new A {S = "zzz", Z = 2}};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertArray()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().U.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    T = new T
                        {
                            R = new[]
                                {
                                    new R {U = new U {S = "zzz1"}},
                                    new R {U = new U {S = "zzz2"}},
                                    new R {U = new U {S = "zzz3"}},
                                }
                        }
                };
            converter(from, to);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "zzz1"},
                                    new B {S = "zzz2"},
                                    new B {S = "zzz3"},
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertArrayCycleRightPartMissing()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.B[0].S).Set(data2 => data2.S);
                    configurator.Target(data => data.A.B[1].S).Set(data2 => data2.T.S);
                    configurator.GoTo(data => data.A.B.Each()).If((x, b) => b.S == "zzz").Target(b => b.S).Set(data2 => "qxx");
                });
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2 {T = new T {S = "qzz"}, S = "zzz"};
            converter(from, to);
            var expected = new TestData {A = new A {B = new[] {new B {S = "qxx"}, new B {S = "qzz"},}}};
            to.AssertEqualsToUsingGrobuf(expected);
            to = new TestData();
            from = new TestData2 {T = new T {S = "zzz"}, S = "qzz"};
            converter(from, to);
            expected = new TestData {A = new A {B = new[] {new B {S = "qzz"}, new B {S = "qxx"},}}};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Category("Failing")]
        [Test(Description = "Unstable, GroboCompiler sporadically fails when run together with all the other tests in the solution")]
        public void TestConvertWithComplexSelectMany()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection,
                                                                              configurator => configurator
                                                                                                  .GoTo(data => data.A.B.Each(),
                                                                                                        data2 => data2.T.R.SelectMany(r => r.Чужь ?? new string[1], (x, y) => new {x, y}).Current())
                                                                                                  .BatchSet((x, y) => new Batch
                                                                                                      {
                                                                                                          {x.S, y.x.S},
                                                                                                          {x.Arr[0], y.y}
                                                                                                      })
                );
            var converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2 {T = new T {R = new[] {new R {S = "zzz"}}}};
            converter(from, to);
            var expected = new TestData {A = new A {B = new[] {new B {S = "zzz", Arr = new string[] {null}}}}};
            to.AssertEqualsToUsingGrobuf(expected);

            to = new TestData();
            from = new TestData2 {T = new T {R = new[] {new R {S = "zzz", Чужь = new[] {"qxx", "qzz"}}}}};
            converter(from, to);
            expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "zzz", Arr = new[] {"qxx"}},
                                    new B {S = "zzz", Arr = new[] {"qzz"}}
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertArrayWithFilter()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    //configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Where(r => r.S != null).Each().U.S)
                    var subConfigurator = configurator.GoTo(data => data.A.B.Each(), data2 => data2.T.Rz.Where(r => r.S != null).Each().U);
                    subConfigurator.Target(b => b.S).Set(u => u.S);
//                    var subConfigurator2 = configurator.GoTo(data => data.A.B.Each(), data2 => data2.T.Rz.Each().U);
//                    subConfigurator2.Target(b => b.S).Set(u => u.S);
                });
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    T = new T
                        {
                            Rz = new[]
                                {
                                    new R {S = "qxx", U = new U {S = "zzz1"}},
                                    new R {U = new U {S = "zzz2"}},
                                    new R {S = "qxx", U = new U {S = "zzz3"}},
                                }
                        }
                };
            converter(from, to);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "zzz1"},
                                    new B {S = "zzz3"},
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertArrayWithStaticMethod()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().C.D.Each().S).Set(data2 => FilterArray(data2.T.R.Each().U.V.X).Each().W.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    T = new T
                        {
                            R = new[]
                                {
                                    new R
                                        {
                                            U = new U
                                                {
                                                    V = new V
                                                        {
                                                            X = new[]
                                                                {
                                                                    new X {S = "qxx", W = new W {S = "zzz1"}},
                                                                    new X {W = new W {S = "zzz2"}},
                                                                    new X {S = "qxx", W = new W {S = "zzz3"}},
                                                                }
                                                        }
                                                }
                                        },
                                    new R
                                        {
                                            U = new U
                                                {
                                                    V = new V
                                                        {
                                                            X = new[]
                                                                {
                                                                    new X {W = new W {S = "zzz4"}},
                                                                    new X {S = "qxx", W = new W {S = "zzz5"}},
                                                                    new X {S = "qxx", W = new W {S = "zzz6"}},
                                                                }
                                                        }
                                                }
                                        },
                                }
                        }
                };
            filterArrayCalls = 0;
            converter(from, to);
            Assert.AreEqual(2, filterArrayCalls);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B
                                        {
                                            C = new C
                                                {
                                                    D = new[]
                                                        {
                                                            new D {S = "zzz1"},
                                                            new D {S = "zzz3"}
                                                        }
                                                }
                                        },
                                    new B
                                        {
                                            C = new C
                                                {
                                                    D = new[]
                                                        {
                                                            new D {S = "zzz5"},
                                                            new D {S = "zzz6"}
                                                        }
                                                }
                                        },
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertArrayWithInstanceMethod()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().C.D.Each().S).Set(data2 => FilterArray2(data2.T.R.Each().U.V.X).Each().W.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    T = new T
                        {
                            R = new[]
                                {
                                    new R
                                        {
                                            U = new U
                                                {
                                                    V = new V
                                                        {
                                                            X = new[]
                                                                {
                                                                    new X {S = "qxx", W = new W {S = "zzz1"}},
                                                                    new X {W = new W {S = "zzz2"}},
                                                                    new X {S = "qxx", W = new W {S = "zzz3"}},
                                                                }
                                                        }
                                                }
                                        },
                                    new R
                                        {
                                            U = new U
                                                {
                                                    V = new V
                                                        {
                                                            X = new[]
                                                                {
                                                                    new X {W = new W {S = "zzz4"}},
                                                                    new X {S = "qxx", W = new W {S = "zzz5"}},
                                                                    new X {S = "qxx", W = new W {S = "zzz6"}},
                                                                }
                                                        }
                                                }
                                        },
                                }
                        }
                };
            filterArrayCalls = 0;
            converter(from, to);
            Assert.AreEqual(2, filterArrayCalls);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B
                                        {
                                            C = new C
                                                {
                                                    D = new[]
                                                        {
                                                            new D {S = "zzz1"},
                                                            new D {S = "zzz3"}
                                                        }
                                                }
                                        },
                                    new B
                                        {
                                            C = new C
                                                {
                                                    D = new[]
                                                        {
                                                            new D {S = "zzz5"},
                                                            new D {S = "zzz6"}
                                                        }
                                                }
                                        },
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertArrayCurrentIndex()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().CurrentIndex().ToString()));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    T = new T
                        {
                            R = new[] {new R {}, new R {}, new R {},}
                        }
                };
            converter(from, to);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "0"},
                                    new B {S = "1"},
                                    new B {S = "2"},
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertSimpleArray()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.Хрень.Each()).Set(data2 => data2.Чужь.Each()));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    Чужь = new[] {"zzz1", "zzz2", "zzz3"}
                };
            converter(from, to);
            var expected = new TestData
                {
                    Хрень = new[] {"zzz1", "zzz2", "zzz3"}
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertSimpleArrayInArray()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().Хрень.Each()).Set(data2 => data2.T.R.Each().Чужь.Each()));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    T = new T
                        {
                            R = new[] {new R {Чужь = new[] {"zzz1", "zzz2", "zzz3"}}, new R(), new R {Чужь = new[] {"qxx1", "qxx2", "qxx3"}}}
                        }
                };
            converter(from, to);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[] {new B {Хрень = new[] {"zzz1", "zzz2", "zzz3"}}, new B(), new B {Хрень = new[] {"qxx1", "qxx2", "qxx3"}}}
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertArrayNull()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().U.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    T = new T()
                };
            converter(from, to);
            var expected = new TestData {};
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertDestArrayIsCutToSource()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().U.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "qxx1"},
                                    new B {S = "qxx2"},
                                    new B {S = "qxx3"},
                                    new B {S = "qxx4"},
                                }
                        }
                };
            var from = new TestData2
                {
                    T = new T
                        {
                            R = new[]
                                {
                                    new R {U = new U {S = "zzz1"}},
                                    new R {U = new U {S = "zzz2"}},
                                    new R {U = new U {S = "zzz3"}},
                                }
                        }
                };
            converter(from, to);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "zzz1"},
                                    new B {S = "zzz2"},
                                    new B {S = "zzz3"},
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        private static string Format(string z)
        {
            ++numberOfCalls;
            return z;
        }

        [Test]
        public void TestCachingExternalExpressions()
        {
            numberOfCalls = 0;
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                                                                                                       configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().U.S + Format(data2.T.S)));
            var converter = collection.GetConverter(MutatorsContext.Empty);

            converter(new TestData2 {T = new T {R = new[] {new R(), new R(), new R(),}}});
            Assert.AreEqual(1, numberOfCalls);
        }

        [Test]
        public void TestConvertDestArrayIsIncreasedToSource()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator => configurator.Target(data => data.A.B.Each().S).Set(data2 => data2.T.R.Each().U.S));
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "qxx1"},
                                    new B {S = "qxx2"},
                                }
                        }
                };
            var from = new TestData2
                {
                    T = new T
                        {
                            R = new[]
                                {
                                    new R {U = new U {S = "zzz1"}},
                                    new R {U = new U {S = "zzz2"}},
                                    new R {U = new U {S = "zzz3"}},
                                }
                        }
                };
            converter(from, to);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "zzz1"},
                                    new B {S = "zzz2"},
                                    new B {S = "zzz3"},
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestConvertArrayExactIndexes()
        {
            var collection = new TestConverterCollection<TestData2, TestData>(pathFormatterCollection, configurator =>
                {
                    configurator.Target(data => data.A.B[0].S).Set(data2 => data2.T.R[0].U.S);
                    configurator.Target(data => data.A.B[1].S).Set(data2 => data2.T.R[1].U.S);
                });
            Action<TestData2, TestData> converter = collection.GetMerger(MutatorsContext.Empty);
            var to = new TestData();
            var from = new TestData2
                {
                    T = new T
                        {
                            R = new[]
                                {
                                    new R {U = new U {S = "zzz1"}},
                                    new R {U = new U {S = "zzz2"}},
                                    new R {U = new U {S = "zzz3"}},
                                }
                        }
                };
            converter(from, to);
            var expected = new TestData
                {
                    A = new A
                        {
                            B = new[]
                                {
                                    new B {S = "zzz1"},
                                    new B {S = "zzz2"},
                                }
                        }
                };
            to.AssertEqualsToUsingGrobuf(expected);
        }

        [Test]
        public void TestRightOrder()
        {
            var expressions = new List<Expression<Func<TestData, int?>>[]>
                {
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.A0, data => data.Qxx.B0, data => data.Qxx.C0, data => data.Qxx.D0},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.A1, data => data.Qxx.B1, data => data.Qxx.D1, data => data.Qxx.C1},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.A2, data => data.Qxx.C2, data => data.Qxx.B2, data => data.Qxx.D2},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.A3, data => data.Qxx.C3, data => data.Qxx.D3, data => data.Qxx.B3},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.A4, data => data.Qxx.D4, data => data.Qxx.B4, data => data.Qxx.C4},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.A5, data => data.Qxx.D5, data => data.Qxx.C5, data => data.Qxx.B5},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.B6, data => data.Qxx.A6, data => data.Qxx.C6, data => data.Qxx.D6},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.B7, data => data.Qxx.A7, data => data.Qxx.D7, data => data.Qxx.C7},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.B8, data => data.Qxx.C8, data => data.Qxx.A8, data => data.Qxx.D8},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.B9, data => data.Qxx.C9, data => data.Qxx.D9, data => data.Qxx.A9},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Ba, data => data.Qxx.Da, data => data.Qxx.Aa, data => data.Qxx.Ca},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Bb, data => data.Qxx.Db, data => data.Qxx.Cb, data => data.Qxx.Ab},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Cc, data => data.Qxx.Ac, data => data.Qxx.Bc, data => data.Qxx.Dc},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Cd, data => data.Qxx.Ad, data => data.Qxx.Dd, data => data.Qxx.Bd},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Ce, data => data.Qxx.Be, data => data.Qxx.Ae, data => data.Qxx.De},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Cf, data => data.Qxx.Bf, data => data.Qxx.Df, data => data.Qxx.Af},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Cg, data => data.Qxx.Dg, data => data.Qxx.Ag, data => data.Qxx.Bg},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Ch, data => data.Qxx.Dh, data => data.Qxx.Bh, data => data.Qxx.Ah},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Di, data => data.Qxx.Ai, data => data.Qxx.Bi, data => data.Qxx.Ci},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Dj, data => data.Qxx.Aj, data => data.Qxx.Cj, data => data.Qxx.Bj},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Dk, data => data.Qxx.Bk, data => data.Qxx.Ak, data => data.Qxx.Ck},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Dl, data => data.Qxx.Bl, data => data.Qxx.Cl, data => data.Qxx.Al},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Dm, data => data.Qxx.Cm, data => data.Qxx.Am, data => data.Qxx.Bm},
                    new Expression<Func<TestData, int?>>[] {data => data.Qxx.Dn, data => data.Qxx.Cn, data => data.Qxx.Bn, data => data.Qxx.An},
                };
            var collection = new TestDataConfiguratorCollection<TestData>(null, null, pathFormatterCollection, configurator => expressions.ForEach(item => SetMutators(configurator, item[0], item[1], item[2], item[3])));
            var o = new TestData {Qxx = new Qxx()};
            expressions.ForEach(item => SetValues(o, 3, 0, 1, 2, item[0], item[1], item[2], item[3]));
            Action<TestData> mutator = collection.GetMutatorsTree(MutatorsContext.Empty).GetTreeMutator();
            mutator(o);
            for (int index = 0; index < expressions.Count; index++)
            {
                Expression<Func<TestData, int?>>[] item = expressions[index];
                Expression<Func<TestData, int?>> a = item[0];
                Expression<Func<TestData, int?>> b = item[1];
                Expression<Func<TestData, int?>> c = item[2];
                Expression<Func<TestData, int?>> d = item[3];
                Assert.AreEqual(3, a.Compile()(o));
                Assert.AreEqual(null, b.Compile()(o));
                Assert.AreEqual(1, c.Compile()(o));
                Assert.AreEqual(2, d.Compile()(o));
            }
        }

        protected override void SetUp()
        {
            base.SetUp();
            pathFormatterCollection = new PathFormatterCollection();
            random = new Random();
        }

        public static X[] FilterArray(X[] arr)
        {
            ++filterArrayCalls;
            return arr.Where(x => x.S != null).ToArray();
        }

        public X[] FilterArray2(X[] arr)
        {
            ++filterArrayCalls;
            return arr.Where(x => x.S != null).ToArray();
        }

        private void SetValues(TestData o, int? aValue, int? bValue, int? cValue, int? dValue, Expression<Func<TestData, int?>> a, Expression<Func<TestData, int?>> b, Expression<Func<TestData, int?>> c, Expression<Func<TestData, int?>> d)
        {
            ParameterExpression p = Expression.Parameter(typeof(TestData));
            a = Expression.Lambda<Func<TestData, TestData>>(p, new[] {p}).Merge(a);
            b = Expression.Lambda<Func<TestData, TestData>>(p, new[] {p}).Merge(b);
            c = Expression.Lambda<Func<TestData, TestData>>(p, new[] {p}).Merge(c);
            d = Expression.Lambda<Func<TestData, TestData>>(p, new[] {p}).Merge(d);
            Action<TestData> action = Expression.Lambda<Action<TestData>>(Expression.Block(new ParameterExpression[0],
                                                                                           Expression.Assign(a.Body, Expression.Constant(aValue, typeof(int?))),
                                                                                           Expression.Assign(b.Body, Expression.Constant(bValue, typeof(int?))),
                                                                                           Expression.Assign(c.Body, Expression.Constant(cValue, typeof(int?))),
                                                                                           Expression.Assign(d.Body, Expression.Constant(dValue, typeof(int?))),
                                                                                           Expression.Empty()), new[] {p}).Compile();
            action(o);
        }

        private void SetMutators(MutatorsConfigurator<TestData> configurator, Expression<Func<TestData, int?>> a, Expression<Func<TestData, int?>> b, Expression<Func<TestData, int?>> c, Expression<Func<TestData, int?>> d)
        {
            ParameterExpression p = Expression.Parameter(typeof(TestData));
            a = Expression.Lambda<Func<TestData, TestData>>(p, new[] {p}).Merge(a);
            b = Expression.Lambda<Func<TestData, TestData>>(p, new[] {p}).Merge(b);
            c = Expression.Lambda<Func<TestData, TestData>>(p, new[] {p}).Merge(c);
            d = Expression.Lambda<Func<TestData, TestData>>(p, new[] {p}).Merge(d);
            if (random.Next(6) > 2)
            {
                configurator.Target(a).NullifyIf(Expression.Lambda<Func<TestData, bool?>>(Expression.Convert(Expression.LessThan(b.Body, c.Body), typeof(bool?)), new[] {p}));
                configurator.Target(b).NullifyIf((Expression.Lambda<Func<TestData, bool?>>(Expression.Convert(Expression.LessThan(c.Body, d.Body), typeof(bool?)), new[] {p})));
            }
            else
            {
                configurator.Target(b).NullifyIf((Expression.Lambda<Func<TestData, bool?>>(Expression.Convert(Expression.LessThan(c.Body, d.Body), typeof(bool?)), new[] {p})));
                configurator.Target(a).NullifyIf((Expression.Lambda<Func<TestData, bool?>>(Expression.Convert(Expression.LessThan(b.Body, c.Body), typeof(bool?)), new[] {p})));
            }
        }

        private static int numberOfCalls;

        private static int filterArrayCalls;

        private Random random;
        private IPathFormatterCollection pathFormatterCollection;

        public class IdGenerator
        {
            public string GetId()
            {
                return Guid.NewGuid().ToString();
            }
        }

        public class TestData
        {
            public string S { get; set; }
            public string F { get; set; }

            public A A { get; set; }
            public Qxx Qxx { get; set; }

            public string[] Хрень { get; set; }

            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public int Q { get; set; }

            public DateTime? Date { get; set; }
        }

        public class A
        {
            public B[] B { get; set; }
            public int? Z { get; set; }
            public string S;
        }

        public class B
        {
            public string S { get; set; }
            public string[] Хрень { get; set; }

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
        }

        public class TestData2
        {
            public string S { get; set; }

            public T T { get; set; }
            public Qxx Qxx { get; set; }

            public string[] Чужь { get; set; }

            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public int Q { get; set; }

            public DateTime? Date1 { get; set; }
            public DateTime? Date2 { get; set; }
        }

        public class T
        {
            public R[] R { get; set; }
            public IEnumerable<R> Rz { get; set; }
            public int? Z { get; set; }
            public string S;
        }

        public class R
        {
            public U U { get; set; }
            public string S { get; set; }

            public string[] Чужь { get; set; }
        }

        public class U
        {
            public string S { get; set; }
            public int? Z { get; set; }
            public string[] Arr { get; set; }
            public V V { get; set; }
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
        }

        public class Qxx
        {
            public int? A0 { get; set; }
            public int? B0 { get; set; }
            public int? C0 { get; set; }
            public int? D0 { get; set; }
            public int? A1 { get; set; }
            public int? B1 { get; set; }
            public int? C1 { get; set; }
            public int? D1 { get; set; }
            public int? A2 { get; set; }
            public int? B2 { get; set; }
            public int? C2 { get; set; }
            public int? D2 { get; set; }
            public int? A3 { get; set; }
            public int? B3 { get; set; }
            public int? C3 { get; set; }
            public int? D3 { get; set; }
            public int? A4 { get; set; }
            public int? B4 { get; set; }
            public int? C4 { get; set; }
            public int? D4 { get; set; }
            public int? A5 { get; set; }
            public int? B5 { get; set; }
            public int? C5 { get; set; }
            public int? D5 { get; set; }
            public int? A6 { get; set; }
            public int? B6 { get; set; }
            public int? C6 { get; set; }
            public int? D6 { get; set; }
            public int? A7 { get; set; }
            public int? B7 { get; set; }
            public int? C7 { get; set; }
            public int? D7 { get; set; }
            public int? A8 { get; set; }
            public int? B8 { get; set; }
            public int? C8 { get; set; }
            public int? D8 { get; set; }
            public int? A9 { get; set; }
            public int? B9 { get; set; }
            public int? C9 { get; set; }
            public int? D9 { get; set; }
            public int? Aa { get; set; }
            public int? Ba { get; set; }
            public int? Ca { get; set; }
            public int? Da { get; set; }
            public int? Ab { get; set; }
            public int? Bb { get; set; }
            public int? Cb { get; set; }
            public int? Db { get; set; }
            public int? Ac { get; set; }
            public int? Bc { get; set; }
            public int? Cc { get; set; }
            public int? Dc { get; set; }
            public int? Ad { get; set; }
            public int? Bd { get; set; }
            public int? Cd { get; set; }
            public int? Dd { get; set; }
            public int? Ae { get; set; }
            public int? Be { get; set; }
            public int? Ce { get; set; }
            public int? De { get; set; }
            public int? Af { get; set; }
            public int? Bf { get; set; }
            public int? Cf { get; set; }
            public int? Df { get; set; }
            public int? Ag { get; set; }
            public int? Bg { get; set; }
            public int? Cg { get; set; }
            public int? Dg { get; set; }
            public int? Ah { get; set; }
            public int? Bh { get; set; }
            public int? Ch { get; set; }
            public int? Dh { get; set; }
            public int? Ai { get; set; }
            public int? Bi { get; set; }
            public int? Ci { get; set; }
            public int? Di { get; set; }
            public int? Aj { get; set; }
            public int? Bj { get; set; }
            public int? Cj { get; set; }
            public int? Dj { get; set; }
            public int? Ak { get; set; }
            public int? Bk { get; set; }
            public int? Ck { get; set; }
            public int? Dk { get; set; }
            public int? Al { get; set; }
            public int? Bl { get; set; }
            public int? Cl { get; set; }
            public int? Dl { get; set; }
            public int? Am { get; set; }
            public int? Bm { get; set; }
            public int? Cm { get; set; }
            public int? Dm { get; set; }
            public int? An { get; set; }
            public int? Bn { get; set; }
            public int? Cn { get; set; }
            public int? Dn { get; set; }
        }
    }
}