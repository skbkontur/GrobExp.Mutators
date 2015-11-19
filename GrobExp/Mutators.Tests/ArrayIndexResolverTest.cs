using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

using GrEmit;

namespace Mutators.Tests
{
    public class ArrayIndexResolverTest : TestBase
    {
        // ReSharper disable PossibleNullReferenceException
        [Test]
        public void Test1()
        {
            Expression<Func<TestData, string>> exp = data => data.A.SingleOrDefault().S;
            Func<TestData, string> func = data => data.A.Select((a, i) => new IndexedValue<A>(a, new[] {i})).SingleOrDefault().Value.S;
            DoTest(exp, new TestData {A = new[] {new A()}}, "A.0.S");
            DoTest(exp, new TestData {A = new A[] {null}}, "A.0.S");
            DoTest(exp, new TestData {A = new A[0]}, "A.-1.S");
            DoTest(exp, new TestData(), "A.-1.S");
            DoTest(exp, null, "A.-1.S");
        }

        [Test]
        public void Test2()
        {
            Expression<Func<TestData, string>> exp = data => data.A.FirstOrDefault().S;
            Func<TestData, string> func = data => data.A.Select((a, i) => new IndexedValue<A>(a, new[] {i})).FirstOrDefault().Value.S;
            DoTest(exp, new TestData {A = new[] {new A()}}, "A.0.S");
            DoTest(exp, new TestData {A = new A[] {null}}, "A.0.S");
            DoTest(exp, new TestData {A = new A[0]}, "A.-1.S");
            DoTest(exp, new TestData(), "A.-1.S");
            DoTest(exp, null, "A.-1.S");
        }

        [Test]
        public void Test3()
        {
            Expression<Func<TestData, string>> exp = data => data.A.FirstOrDefault(a => a.X > 0).S;
            Func<TestData, string> func = data => data.A.Select((a, i) => new IndexedValue<A>(a, new[] {i})).FirstOrDefault(value => value.Value.X > 0).Value.S;
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1},}}, "A.1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1},}}, "A.-1.S");
            DoTest(exp, new TestData {A = new[] {new A(),}}, "A.-1.S");
            DoTest(exp, new TestData {A = new A[] {null}}, "A.-1.S");
            DoTest(exp, new TestData {A = new A[0]}, "A.-1.S");
            DoTest(exp, new TestData(), "A.-1.S");
            DoTest(exp, null, "A.-1.S");
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
            Func<TestData, string> func = data =>
                                              {
                                                  var indexes = new List<int>();
                                                  var temp1 = data.A.Select((a, i) => new IndexedValue<A>(a, new[] {i})).Where(value => value.Value.X > 0).FirstOrDefault();
                                                  indexes.AddRange(temp1.Indexes);
                                                  var temp2 = temp1.Value.B.Select((b, j) => new IndexedValue<B>(b, new[] {j})).FirstOrDefault(value => value.Value.X > 0);
                                                  indexes.AddRange(temp2.Indexes);
                                                  return new IndexedValue<string>(temp2.Value.S, indexes).Value;
                                              };
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new[] {new B {X = -1}, new B {X = 1}}}}}, "A.1.B.1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new[] {new B {X = -1}}}}}, "A.1.B.-1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new[] {new B()}}}}, "A.1.B.-1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new B[] {null}}}}, "A.1.B.-1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1, B = new B[0]}}}, "A.1.B.-1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1}, new A {X = 1}}}, "A.1.B.-1.S");
            DoTest(exp, new TestData {A = new[] {new A {X = -1}}}, "A.-1.B.-1.S");
            DoTest(exp, new TestData {A = new A[] {null}}, "A.-1.B.-1.S");
            DoTest(exp, new TestData {A = new A[0]}, "A.-1.B.-1.S");
            DoTest(exp, new TestData(), "A.-1.B.-1.S");
            DoTest(exp, null, "A.-1.B.-1.S");
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

        private bool Possible(int p, int sum, int k, int[] counts, int[] prices, int[] maxes, int[] zzz)
        {
            if(sum < 0)
                return false;
            if(sum == 0)
                return true;
            if(k < 0)
                return false;
            for (int i = 1; i <= maxes[k]; ++i)
            {
                sum -= prices[k];
                if(counts[k] < i * p)
                    return false;
                if(Possible(p, sum, k - 1, counts, prices, maxes, zzz))
                {
                    zzz[k] = i;
                    return true;
                }
            }
            return false;
        }

        [Test, Ignore]
        public void Test()
        {
            var counts = new[] {300, 181, 240, 175, 44};
            var prices = new[] {5, 25, 100, 500, 2500};
            var maxes = new[] {10, 10, 10, 3, 1};
            var result = new int[5];
            Console.WriteLine(Possible(32, 5000, 4, counts, prices, maxes, result));
            for(int i = 4; i >= 0; --i)
                Console.WriteLine("{0} : {1}", prices[i], result[i]);
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

        // ReSharper restore PossibleNullReferenceException

        private void DoTest<T1, T2>(Expression<Func<T1, T2>> exp, T1 data, string expected)
        {
            ParameterExpression[] currentIndexes;
            var body = new LinqEliminator().Eliminate(exp.Body, out currentIndexes);
            var paths = ExpressionPathsBuilder.BuildPaths(exp.Body, currentIndexes);
            var resolved = Expression.Block(currentIndexes, new[]
            {
                body,
                paths
            });
            ParameterExpression[] parameters = resolved.ExtractParameters();
            Expression<Func<T1, string[][]>> lambda = Expression.Lambda<Func<T1, string[][]>>(resolved, parameters);
            Assert.AreEqual(expected, string.Join(".", LambdaCompiler.Compile(lambda, CompilerOptions.All)/*.Compile()*/(data)[0]));
        }

        public class IndexedValue<T>
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
        }

        public class A
        {
            public string S { get; set; }
            public int X { get; set; }
            public B[] B { get; set; }
        }

        public class B
        {
            public string S { get; set; }
            public int X { get; set; }
            public C[] C { get; set; }
        }

        public class C
        {
            public string S { get; set; }
            public int X { get; set; }
        }
    }
}