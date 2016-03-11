using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class LinqEliminatorTest : TestBase
    {
        // ReSharper disable PossibleNullReferenceException
        [Test]
        public void TestSingleOrDefault()
        {
            Expression<Func<TestData, string>> exp = data => data.A.SingleOrDefault().S;
            var func = EliminateLinq(exp);
            Assert.AreEqual("zzz", func(new TestData {A = new A[] {new A{S = "zzz"}, }}));
            Assert.Throws<InvalidOperationException>(() => func(new TestData {A = new A[] {new A(), new A(), }}));
            Assert.IsNull(func(new TestData {A = new A[0]}));
        }

        [Test]
        public void TestSingle()
        {
            Expression<Func<TestData, string>> exp = data => data.A.Single().S;
            var func = EliminateLinq(exp);
            Assert.AreEqual("zzz", func(new TestData {A = new A[] {new A{S = "zzz"}, }}));
            Assert.Throws<InvalidOperationException>(() => func(new TestData {A = new A[0]}));
            Assert.Throws<InvalidOperationException>(() => func(new TestData()));
        }

        [Test]
        public void TestFirstOrDefault1()
        {
            Expression<Func<TestData, string>> exp = data => data.A.FirstOrDefault().S;
            var withoutLinq = EliminateLinq(exp);
            withoutLinq(new TestData {A = new[] {new A(),}});
        }

        [Test]
        public void TestFirstOrDefault2()
        {
            Expression<Func<TestData, string>> exp = data => data.A.FirstOrDefault(a => a.X > 0).S;
            var withoutLinq = EliminateLinq(exp);
            withoutLinq(new TestData {A = new[] {new A(),}});
        }

        [Test]
        public void TestAny1()
        {
            Expression<Func<TestData, bool>> exp = data => data.A.Any(a => a.X > 0);
            var withoutLinq = EliminateLinq(exp);
            Assert.IsFalse(withoutLinq(new TestData {A = new[] {new A(),}}));
            Assert.IsTrue(withoutLinq(new TestData {A = new[] {new A{X = 1},}}));
        }

        [Test]
        public void TestAny2()
        {
            Expression<Func<TestData, bool>> exp = data => data.A.Any();
            var withoutLinq = EliminateLinq(exp);
            Assert.IsTrue(withoutLinq(new TestData {A = new[] {new A(),}}));
        }

        [Test]
        public void TestContains1()
        {
            Expression<Func<TestData, bool>> exp = data => data.Strings.Contains("zzz");
            var withoutLinq = EliminateLinq(exp);
            Assert.IsFalse(withoutLinq(new TestData { }));
            Assert.IsFalse(withoutLinq(new TestData {Strings = new [] {"qxx"}}));
            Assert.IsTrue(withoutLinq(new TestData {Strings = new [] {"zzz"}}));
        }

        [Test]
        public void TestAll1()
        {
            Expression<Func<TestData, bool>> exp = data => data.A.All(a => a.X > 0);
            var withoutLinq = EliminateLinq(exp);
            Assert.IsFalse(withoutLinq(new TestData { A = new[] { new A(), } }));
            Assert.IsTrue(withoutLinq(new TestData { A = new[] { new A { X = 1 }, } }));
            Assert.IsFalse(withoutLinq(new TestData { A = new[] { new A { X = 1 }, new A { X = -1 } } }));
        }

        [Test]
        public void TestSum1()
        {
            Expression<Func<TestData, int>> exp = data => data.A.Sum(a => a.X);
            var withoutLinq = EliminateLinq(exp);
            Assert.AreEqual(0, withoutLinq(new TestData()));
            Assert.AreEqual(0, withoutLinq(new TestData{A = new[] {new A(), }}));
            Assert.AreEqual(3, withoutLinq(new TestData { A = new[] { new A { X = 1 }, new A { X = 2 } } }));
        }

        [Test]
        public void TestSum2()
        {
            Expression<Func<TestData, int?>> exp = data => data.A.Sum(a => a.Y);
            var withoutLinq = EliminateLinq(exp);
            Assert.AreEqual(0, withoutLinq(new TestData()));
            Assert.AreEqual(0, withoutLinq(new TestData{A = new[] {new A(), }}));
            Assert.AreEqual(3, withoutLinq(new TestData { A = new[] { new A { Y = 1 }, new A { Y = 2 }, new A() } }));
        }

        [Test]
        public void TestCount1()
        {
            Expression<Func<TestData, int>> exp = data => data.A.Select(a => a).Count();
            var withoutLinq = EliminateLinq(exp);
            Assert.AreEqual(0, withoutLinq(new TestData()));
            Assert.AreEqual(1, withoutLinq(new TestData{A = new[] {new A(), }}));
            Assert.AreEqual(2, withoutLinq(new TestData{A = new[] {new A(), new A{X = -1}, }}));
        }

        [Test]
        public void TestCount2()
        {
            Expression<Func<TestData, int>> exp = data => data.A.Count(a => a.X > 0);
            var withoutLinq = EliminateLinq(exp);
            Assert.AreEqual(0, withoutLinq(new TestData()));
            Assert.AreEqual(0, withoutLinq(new TestData{A = new[] {new A(), }}));
            Assert.AreEqual(0, withoutLinq(new TestData{A = new[] {new A(), new A{X = -1}, }}));
            Assert.AreEqual(2, withoutLinq(new TestData { A = new[] { new A { X = 1 }, new A { X = -2 } , new A { X = 2 } } }));
        }

        [Test]
        public void TestAggregate1()
        {
            Expression<Func<TestData, string>> exp = data => data.Strings.Aggregate((s1, s2) => s1 + s2);
            var withoutLinq = EliminateLinq(exp);

            Assert.Throws<InvalidOperationException>(() => withoutLinq(new TestData()));
            Assert.Throws<InvalidOperationException>(() => withoutLinq(new TestData{Strings = new string[0]}));
            Assert.AreEqual("zzz", withoutLinq(new TestData{Strings = new[] {"zzz"}}));
            Assert.AreEqual("zzzqxx", withoutLinq(new TestData{Strings = new[] {"zzz", "qxx"}}));
        }

        [Test]
        public void TestAggregate2()
        {
            Expression<Func<TestData, decimal>> exp = data => data.A.Aggregate(0m, (x, a) => x + a.Z);
            var withoutLinq = EliminateLinq(exp);

            Assert.AreEqual(0m, withoutLinq(new TestData()));
            Assert.AreEqual(0m, withoutLinq(new TestData{A = new A[0]}));
            Assert.AreEqual(1m, withoutLinq(new TestData{A = new [] {new A{Z = 1m}}}));
            Assert.AreEqual(3m, withoutLinq(new TestData { A = new[] { new A { Z = 1m }, new A { Z = 2m } } }));
        }

        [Test]
        public void TestAggregate3()
        {
            Expression<Func<TestData, string>> exp = data => data.A.Aggregate(0m, (x, a) => x + a.Z, z => z.ToString());
            var withoutLinq = EliminateLinq(exp);

            Assert.AreEqual("0", withoutLinq(new TestData()));
            Assert.AreEqual("0", withoutLinq(new TestData { A = new A[0] }));
            Assert.AreEqual("1", withoutLinq(new TestData { A = new[] { new A { Z = 1m } } }));
            Assert.AreEqual("3", withoutLinq(new TestData { A = new[] { new A { Z = 1m }, new A { Z = 2m } } }));
        }

        [Test]
        public void TestSelectWithIndex()
        {
            Expression<Func<TestData, string>> exp = data => data.A.Where(a => a.X > 0).Select((a, i) => a.S + "_" + (i + 1)).First();
            var func = EliminateLinq(exp);
            Assert.AreEqual("zzz_1", func(new TestData {A = new[] {new A {X = -1}, new A {X = 0}, new A {X = 1, S = "zzz"}}}));
        }

        [Test]
        public void TestSelectManyWithIndex()
        {
            Expression<Func<TestData, string>> exp = data => data.A.SelectMany(a => a.B).Select((b, i) => new {b.X, i}).Where(arg => arg.X > 0).Select(arg => arg.i.ToString()).First();
            var func = EliminateLinq(exp);
            Assert.AreEqual("6", func(new TestData
                {
                    A = new[]
                        {
                            new A
                                {
                                    B = new[]
                                        {
                                            new B(),
                                            new B(),
                                            new B(), 
                                        }
                                },
                            new A
                                {
                                    B = new[]
                                        {
                                            new B(), 
                                        }
                                }, 
                            new A
                                {
                                    B = new[]
                                        {
                                            new B(),
                                            new B(), 
                                            new B{X = 1},
                                            new B{X = 2}, 
                                        }
                                }, 
                        }
                }));
        }

        [Test]
        public void TestSelectManyWithResultSelector()
        {
            Expression<Func<TestData, string>> exp = data => data.A.SelectMany(a => a.B, (a, b) => a.S + b.S).FirstOrDefault(s => s.Length > 3);
            var func = EliminateLinq(exp);
        }

        [Test]
        public void TestSelectManyWithResultSelectorConcat()
        {
            LambdaCompiler.DebugOutputDirectory = @"c:\temp";
            Expression<Func<TestData, string>> exp = data => data.A.SelectMany(a => a.B.Concat(a.B), (a, b) => a.S + b.S).FirstOrDefault(s => s.Length > 3);
            var func = EliminateLinq(exp);
        }

        [Test]
        public void TestSelectManyCollectionSelectorNotChain1()
        {
            Expression<Func<TestData, string>> exp = data => data.A.SelectMany(a => new[] {a.B1, a.B2}).FirstOrDefault(b => b.S == "zzz").S;
            var func = EliminateLinq(exp);
        }

        [Test]
        public void TestSelectManyCollectionSelectorNotChain2()
        {
            Expression<Func<TestData, List<int>>> exp = data => data.A.SelectMany(a => Zzz(a)).ToList();
            var func = EliminateLinq(exp);
            func(new TestData());
        }

        private static int[] Zzz(A a)
        {
            return new[] {a.Y ?? 0, a.X};
        }

        [Test]
        public void Test4()
        {
            Expression<Func<TestData, string>> exp = data => data.A.Where(a => a.X > 0).FirstOrDefault().S;
            Func<TestData, string> func = data => data.A.Select((a, i) => new IndexedValue<A>(a, new[] {i})).Where(value => value.Value.X > 0).FirstOrDefault().Value.S;
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1},}}, "A.1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1},}}, "A.-1.S");
            DoTest(exp, new TestData {A = new[] {new A(),}}, "A.-1.S");
            DoTest(exp, new TestData {A = new A[] {null}}, "A.-1.S");
            DoTest(exp, new TestData {A = new A[0]}, "A.-1.S");
            DoTest(exp, new TestData(), "A.-1.S");
            DoTest(exp, null, "A.-1.S");
        }

        [Test]
        public void Test5()
        {
            Expression<Func<TestData, string>> exp = data => data.A.Where(a => a.X > 0).FirstOrDefault().B.FirstOrDefault(b => b.X > 0).S;
            var withoutLinq = EliminateLinq(exp);
            withoutLinq(new TestData {A = new[] {new A(),}});
//            Func<TestData, string> func = data =>
//                                              {
//                                                  var indexes = new List<int>();
//                                                  var temp1 = data.A.Select((a, i) => new IndexedValue<A>(a, new[] {i})).Where(value => value.Value.X > 0).FirstOrDefault();
//                                                  indexes.AddRange(temp1.Indexes);
//                                                  var temp2 = temp1.Value.B.Select((b, j) => new IndexedValue<B>(b, new[] {j})).FirstOrDefault(value => value.Value.X > 0);
//                                                  indexes.AddRange(temp2.Indexes);
//                                                  return new IndexedValue<string>(temp2.Value.S, indexes).Value;
//                                              };
//            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new[] {new B {X = -1}, new B {X = 1}}}}}, "A.1.B.1.S");
//            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new[] {new B {X = -1}}}}}, "A.1.B.-1.S");
//            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new[] {new B()}}}}, "A.1.B.-1.S");
//            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new B[] {null}}}}, "A.1.B.-1.S");
//            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new B[0]}}}, "A.1.B.-1.S");
//            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1}}}, "A.1.B.-1.S");
//            DoTest(exp, new TestData {A = new[] {new A {X = -1}}}, "A.-1.B.-1.S");
//            DoTest(exp, new TestData {A = new A[] {null}}, "A.-1.B.-1.S");
//            DoTest(exp, new TestData {A = new A[0]}, "A.-1.B.-1.S");
//            DoTest(exp, new TestData(), "A.-1.B.-1.S");
//            DoTest(exp, null, "A.-1.B.-1.S");
        }

        [Test]
        public void Test6()
        {
            Expression<Func<TestData, string>> exp = data => (from a in data.A where a.X > 0 select a.S).FirstOrDefault();
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1},}}, "A.1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1},}}, "A.-1.S");
            DoTest(exp, new TestData {A = new[] {new A(),}}, "A.-1.S");
            DoTest(exp, new TestData {A = new A[] {null}}, "A.-1.S");
            DoTest(exp, new TestData {A = new A[0]}, "A.-1.S");
            DoTest(exp, new TestData(), "A.-1.S");
            DoTest(exp, null, "A.-1.S");
        }

        [Test]
        public void Test7()
        {
            Expression<Func<TestData, string>> exp = data => data.A.Select(a => a.S).Where(s => s != "zzz").FirstOrDefault();
            DoTest(exp, new TestData {A = new[] {new A {S = "zzz"}, new A {S = "qxx"}}}, "A.1.S");
            DoTest(exp, new TestData {A = new[] {new A {S = "zzz"}}}, "A.-1.S");
            DoTest(exp, new TestData {A = new[] {new A()}}, "A.0.S");
            DoTest(exp, new TestData {A = new A[] {null}}, "A.0.S");
            DoTest(exp, new TestData {A = new A[0]}, "A.-1.S");
            DoTest(exp, new TestData(), "A.-1.S");
            DoTest(exp, null, "A.-1.S");
        }

        [Test]
        public void Test8()
        {
            Expression<Func<TestData, string>> exp = data => (from b in data.A.SingleOrDefault().B where b.X > 0 select b.S).FirstOrDefault();
            DoTest(exp, new TestData {A = new[] {new A {B = new[] {new B {X = -1}, new B {X = 1}}}}}, "A.0.B.1.S");
            DoTest(exp, new TestData {A = new[] {new A {B = new[] {new B {X = -1}, new B {X = -1}}}}}, "A.0.B.-1.S");
            DoTest(exp, new TestData {A = new[] {new A {B = new B[] {null, null}}}}, "A.0.B.-1.S");
            DoTest(exp, new TestData {A = new[] {new A {B = new B[0]}}}, "A.0.B.-1.S");
            DoTest(exp, new TestData {A = new[] {new A()}}, "A.0.B.-1.S");
            DoTest(exp, new TestData {A = new A[] {null}}, "A.0.B.-1.S");
            DoTest(exp, new TestData {A = new A[0]}, "A.-1.B.-1.S");
            DoTest(exp, new TestData(), "A.-1.B.-1.S");
            DoTest(exp, null, "A.-1.B.-1.S");
        }

        [Test]
        public void Test9()
        {
            Expression<Func<TestData, string>> exp = data => (from a in data.A where a.X > 0 where a.X < 10 select a.S).FirstOrDefault();
            DoTest(exp, new TestData { A = new[] { new A {X = -1}, new A {X = 11}, new A {X = 1} } }, "A.2.S");
            DoTest(exp, new TestData { A = new[] { new A {X = -1}, new A {X = 11} } }, "A.-1.S");
            DoTest(exp, new TestData { A = new[] { new A {X = -1} } }, "A.-1.S");
            DoTest(exp, new TestData { A = new A[] { null } }, "A.-1.S");
            DoTest(exp, new TestData { A = new A[0] }, "A.-1.S");
            DoTest(exp, new TestData(), "A.-1.S");
            DoTest(exp, null, "A.-1.S");
        }

        [Test]
        public void Test10()
        {
            Expression<Func<TestData, string>> exp = data => data.A.Select(a => a.B.FirstOrDefault(b => b.X > 0).S).FirstOrDefault();
            DoTest(exp, new TestData { A = new[] { new A {B = new[] {new B {X = -1}, new B {X = 1}}}, new A {B = new[] {new B {X = 1}, new B {X = -1}}}} }, "A.0.B.1.S");
            DoTest(exp, new TestData { A = new[] { new A {B = new[] {new B {X = -1}, new B {X = -1}}}, new A {B = new[] {new B {X = 1}, new B {X = -1}}}} }, "A.0.B.-1.S");
        }

        [Test]
        public void Test11()
        {
            Expression<Func<TestData, string>> exp = data => data.A.Where(a => a.S == "zzz").Select(a => a.B.FirstOrDefault(b => b.X > 0).S).FirstOrDefault();
            var testData = new TestData
                {
                    A = new[]
                        {
                            new A {B = new[] {new B {X = -1}, new B {X = 1}}, S = "zzz"},
                            new A {B = new[] {new B {X = 1}, new B {X = -1},}, S = "zzz"},
                        }
                };
            DoTest(exp, testData, "A.0.B.1.S");
        }

        [Test]
        public void Test12()
        {
            Expression<Func<TestData, string>> exp = data => data.A.SelectMany(a => a.B).Where(b => b.X > 0).Select(b => b.S).FirstOrDefault();
            Func<TestData, string> func = data => data.A
                                                      .Select((a, i) => new IndexedValue<A>(a, new[] {i}))
                                                      .SelectMany(valueA =>
                                                                      {
                                                                          var indexes = new List<int>();
                                                                          var temp = valueA.Value.B.Select((b, j) => new IndexedValue<B>(b, new[] {j}));
                                                                          return temp.Select(valueB => new IndexedValue<B>(valueB.Value, valueA.Indexes.Concat(indexes).Concat(valueB.Indexes)));
                                                                      })
                                                      .Where(value => value.Value.X > 0)
                                                      .Select(value => new IndexedValue<string>(value.Value.S, value.Indexes))
                                                      .FirstOrDefault().Value;

            var testData = new TestData
                {
                    A = new[]
                        {
                            new A {B = new[] {new B {X = -1},}},
                            new A {B = new[] {new B {X = 1}, new B {X = -1},}},
                        }
                };
            DoTest(exp, testData, "A.1.B.0.S");
        }

        [Test]
        public void Test13()
        {
            Expression<Func<TestData, string>> exp = data => data.A.SelectMany(a => a.B.Where(b => b.S == "zzz")).Where(b => b.X > 0).Select(b => b.S).FirstOrDefault();
            //Expression<Func<TestData, string>> exp2 = data => data.A.Select((a, i) => new Tuple<A, int[]>(a, new[] {i})).SelectMany(tuple => tuple.Item1.B.Select((b, i) => new Tuple<B, int[]>(b, new[] {i})).Where(tuple2 => tuple2.Item1.S == "zzz"), (tuple1, tuple2) => new Tuple<Tuple<B, int>, int>(tuple2, tuple1.Item2)).Where(tuple => tuple.Item1.Item1.X > 0).Select(tuple => new Tuple<Tuple<string, int>, int>(new Tuple<string, int>(tuple.Item1.Item1.S, tuple.Item1.Item2), tuple.Item2)).FirstOrDefault().Item1.Item1;
            //Expression<Func<TestData, string>> exp2 = data => data.A.Select((a, i) => new Tuple<A, int>(a, i)).SelectMany(tuple => tuple.Item1.B.Select((b, i) => new Tuple<B, int>(b, i)), (tuple1, tuple2) => new Tuple<Tuple<B, int>, int>(tuple2, tuple1.Item2)).Where(tuple => tuple.Item1.Item1.X > 0).Select(tuple => new Tuple<Tuple<string, int>, int>(new Tuple<string, int>(tuple.Item1.Item1.S, tuple.Item1.Item2), tuple.Item2)).FirstOrDefault().Item1.Item1;

            var testData = new TestData
                {
                    A = new[]
                        {
                            new A {B = new[] {new B {X = -1, S = "zzz"},}},
                            new A {B = new[] {new B {X = 1, S = "zzz"}, new B {X = -1, S = "zzz"},}},
                        }
                };
            DoTest(exp, testData, "A.1.B.0.S");
        }

        [Test]
        public void Test14()
        {
            Expression<Func<TestData, string>> exp = data => data.A.SelectMany(a => a.B.FirstOrDefault(b => b.S == "zzz").C).Where(c => c.X > 0).Select(c => c.S).FirstOrDefault();
            Func<TestData, string> exp2 =
                data => data.A
                            .Select((a, i) => new Tuple<A, int[]>(a, new[] {i}))
                            .SelectMany(tupleA =>
                                            {
                                                Tuple<B, int[]> temp = tupleA.Item1.B.Select((b, j) => new Tuple<B, int[]>(b, new[] {j})).FirstOrDefault(tupleB => tupleB.Item1.S == "zzz");
                                                return temp.Item1.C.Select((c, k) => new Tuple<C, int[]>(c, tupleA.Item2.Concat(temp.Item2).Concat(new[] {k}).ToArray()));
                                            })
                            .Where(tuple => tuple.Item1.X > 0)
                            .Select(tuple => new Tuple<string, int[]>(tuple.Item1.S, tuple.Item2))
                            .FirstOrDefault().Item1;
            //Expression<Func<TestData, string>> exp2 = data => data.A.Select((a, i) => new Tuple<A, int[]>(a, new[] {i})).SelectMany(tuple => tuple.Item1.B.Select((b, i) => new Tuple<B, int[]>(b, new[] {i})), (tuple1, tuple2) => new Tuple<Tuple<B, int>, int>(tuple2, tuple1.Item2)).Where(tuple => tuple.Item1.Item1.X > 0).Select(tuple => new Tuple<Tuple<string, int>, int>(new Tuple<string, int>(tuple.Item1.Item1.S, tuple.Item1.Item2), tuple.Item2)).FirstOrDefault().Item1.Item1;
            //Expression<Func<TestData, string>> exp2 = data => data.A.Select((a, i) => new Tuple<A, int>(a, i)).SelectMany(tuple => tuple.Item1.B.Select((b, i) => new Tuple<B, int>(b, i)), (tuple1, tuple2) => new Tuple<Tuple<B, int>, int>(tuple2, tuple1.Item2)).Where(tuple => tuple.Item1.Item1.X > 0).Select(tuple => new Tuple<Tuple<string, int>, int>(new Tuple<string, int>(tuple.Item1.Item1.S, tuple.Item1.Item2), tuple.Item2)).FirstOrDefault().Item1.Item1;

            var testData = new TestData
                {
                    A = new[]
                        {
                            new A {B = new[] {new B {S = "qxx"}, new B {S = "zzz", C = new[] {new C {X = -1},}},}},
                            new A {B = new[] {new B {S = "zzz", C = new[] {new C(), new C {X = 1, S = "qxx"},}},}},
                        }
                };
            DoTest(exp, testData, "A.1.B.0.C.1.S");
        }

        [Test]
        public void Test15()
        {
            Expression<Func<TestData, string>> exp = data => (from a in data.A from b in a.B where b.X > 0 select b.S).FirstOrDefault();
//            Func<TestData, string> func = data =>
//                                              {
//                                                  data.A.Select((a, i) => new IndexedValue<A>(a, new[] {i}))
//                                                        .SelectMany(value =>
//                                                                        {
//                                                                            var indexes = new List<int>(value.Indexes);
//
//                                                                        })
//                                              }
            //Expression<Func<TestData, string>> exp = data => (data.A.SelectMany(a => a.B, (a, b) => new {a, b}).Where(@t => @t.b.X > 0).Select(@t => @t.b.S)).FirstOrDefault();
            var testData = new TestData
                {
                    A = new[]
                        {
                            new A {B = new[] {new B {X = -1}}},
                            new A {B = new[] {new B {X = 1}, new B {X = -1}}},
                        }
                };
            DoTest(exp, testData, "A.1.B.0.S");
        }

        [Test]
        public void Test16()
        {
            Expression<Func<TestData, int>> exp = data => data.A.SelectMany(a => a.B, (a, b) => b.C.FirstOrDefault(c => c.S == "zzz").X).Where(i => i > 0).FirstOrDefault();
            //Expression<Func<TestData, string>> exp = data => (data.A.SelectMany(a => a.B, (a, b) => new {a, b}).Where(@t => @t.b.X > 0).Select(@t => @t.b.S)).FirstOrDefault();
            var testData = new TestData
                {
                    A = new[]
                        {
                            new A {B = new[] {new B {C = new[] {new C {S = "qxx"}, new C {S = "zzz", X = -1}}}, new B {C = new[] {new C {S = "zzz", X = 1},}},}},
                            new A {B = new[] {new B {C = new[] {new C {S = "qxx"}, new C {S = "zzz", X = -1}}}, new B {C = new[] {new C {S = "zzz", X = 1},}},}},
                        }
                };
            DoTest(exp, testData, "A.0.B.1.C.0.X");
        }

        [Test]
        public void Test17()
        {
            Expression<Func<TestData, int>> exp = data => data.A.SelectMany(a => a.B, (a, b) => b.C.FirstOrDefault().X).Where(i => i > 0).FirstOrDefault();
            //Expression<Func<TestData, string>> exp = data => (data.A.SelectMany(a => a.B, (a, b) => new {a, b}).Where(@t => @t.b.X > 0).Select(@t => @t.b.S)).FirstOrDefault();
            var testData = new TestData
                {
                    A = new[]
                        {
                            new A {B = new[] {new B {C = new[] {new C {S = "qxx"}, new C {S = "zzz", X = -1}}}, new B {C = new[] {new C {S = "zzz", X = 1},}},}},
                            new A {B = new[] {new B {C = new[] {new C {S = "qxx"}, new C {S = "zzz", X = -1}}}, new B {C = new[] {new C {S = "zzz", X = 1},}},}},
                        }
                };
            DoTest(exp, testData, "A.0.B.1.C.0.X");
        }

        [Test]
        public void Test18()
        {
            Expression<Func<TestData, int>> exp = data => data.A.Where(a => a.S == "zzz").FirstOrDefault().B.Where(b => b.S == "qxx").Select(b => b.C.FirstOrDefault(c => c.S == "qzz").X + b.C.FirstOrDefault(c => c.S == "xxx").Y).FirstOrDefault();
            var withoutLinq = EliminateLinq(exp);
        }

        [Test, Ignore]
        public void TestPerformance()
        {
            var list = new List<int>();
            for (int i = 0; i < 1000000; ++i)
                list.Add(i);
            Func<List<int>, int> func1 = x => x.Count(z => z > 10000);
            Expression<Func<List<int>, int>> exp = x => x.Count(z => z > 10000);
            var func2 = LambdaCompiler.Compile(exp.EliminateLinq(), CompilerOptions.None);
            Console.WriteLine(func1(list));
            Console.WriteLine(func2(list));
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; ++i)
                func2(list);
            Console.WriteLine(stopwatch.Elapsed.TotalSeconds);
            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < 100; ++i)
                func1(list);
            Console.WriteLine(stopwatch.Elapsed.TotalSeconds);
        }

        // ReSharper restore PossibleNullReferenceException

        private Func<T1, T2> EliminateLinq<T1, T2>(Expression<Func<T1, T2>> exp)
        {
            var withoutLinq = (Expression<Func<T1, T2>>)new LinqEliminator().Visit(exp);
            return LambdaCompiler.Compile(withoutLinq, CompilerOptions.All);
        }

        private void DoTest<T1, T2>(Expression<Func<T1, T2>> exp, T1 data, string expected)
        {
            var withoutLinq = (Expression<Func<T1, T2>>)new LinqEliminator().Visit(exp);
            Console.WriteLine(withoutLinq);
//            var resolved = exp.Body.ResolveArrayIndexes()/*.ExtendNulls()*/;
//            ParameterExpression[] parameters = resolved.ExtractParameters();
//            Expression<Func<T1, string[]>> lambda = Expression.Lambda<Func<T1, string[]>>(resolved, parameters);
//            Assert.AreEqual(expected, string.Join(".", LambdaCompiler.Compile(lambda, CompilerOptions.All)/*.Compile()*/(data)));
        }

        private class IndexedValue<T>
        {
            public IndexedValue(T value, IEnumerable<int> indexes)
            {
                Value = value;
                Indexes = indexes.ToArray();
            }

            public T Value { get; set; }
            public int[] Indexes { get; set; }
        }

        public class TestData
        {
            public A[] A { get; set; }
            public string[] Strings { get; set; }
        }

        public class A
        {
            public string S { get; set; }
            public int X { get; set; }
            public int? Y { get; set; }
            public decimal Z { get; set; }
            public B[] B { get; set; }
            public B B1 { get; set; }
            public B B2 { get; set; }
        }

        public class B
        {
            public string S { get; set; }
            public int X { get; set; }
            public decimal Z { get; set; }
            public C[] C { get; set; }
        }

        public class C
        {
            public string S { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public decimal Z { get; set; }
        }
    }
}