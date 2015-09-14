using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class DependenciesExtractorTest : TestBase
    {
        [Test]
        public void TestUnique()
        {
            Expression<Func<A, B, string>> expression = (a, b) => a.B.C[1].D.E[0].F + b.C[1].D.E[10].Z + a.B.C[1].D.E[0].F;
            DoTest(expression, (a, b) => a.B.C[1].D.E[0].F, (a, b) => b.C[1].D.E[10].Z);
        }

        [Test]
        public void TestBlock()
        {
            Expression<Func<A, string>> expression1 = a => a.B.C[1].D.E[0].F;
            Expression<Func<A, string>> expression2 = a => a.B.C[0].D.E[1].F;
            Expression<Func<A, string>> expression = Expression.Lambda<Func<A, string>>(Expression.Block(expression1.Body, new ParameterReplacer(expression2.Parameters.Single(), expression1.Parameters.Single()).Visit(expression2.Body)), expression1.Parameters);
            DoTest(expression, (a) => a.B.C[1].D.E[0].F, a => a.B.C[0].D.E[1].F);
        }

        [Test]
        public void TestParameter()
        {
            Expression<Func<string, string>> expression = s => s;
            DoTest(expression, s => s);
        }

        [Test]
        public void TestWhere1()
        {
            Expression<Func<A, int?>> expression = a => a.B.C.Where(c => c.D.S == "zzz").Sum(c => c.D.E.Where(e => e.F == "qxx").Sum(e => e.X));
            DoTest(expression, a => a.B.C.Each().D.S, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestWhere2()
        {
            Expression<Func<A, int?>> expression = a => a.B.C.Where(c => c.D.S == "zzz").Sum(c => c.D.E.Single(e => e.F == "qxx").X);
            DoTest(expression, a => a.B.C.Each().D.S, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestWhere3()
        {
            Expression<Func<A, string>> expression = a => a.B.Cz.Where(r => r.S != null).Each().D.S;
            DoTest(expression, a => a.B.Cz.Each().S, a => a.B.Cz.Each().D.S);
        }

        [Test]
        public void TestWhereNull()
        {
            Expression<Func<A, int?>> expression = a => a.B.C.Where(c => null == "zzz").Sum(c => c.D.E.Where(e => null == "qxx").Sum(e => e.X));
            DoTest(expression, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestAny1()
        {
            Expression<Func<A, bool>> expression = a => a.B.C.Any(c => c.S == "zzz");
            DoTest(expression, a => a.B.C.Each().S);
        }

        [Test]
        public void TestAny2()
        {
            Expression<Func<A, int?>> expression = a => a.B.C.Where(c => c.D.E.Any(e => e.F == "zzz")).Sum(c => c.X);
            DoTest(expression, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().X);
        }

        [Test]
        public void TestAny3()
        {
            Expression<Func<A, bool>> expression = a => a.B.C.Any();
            DoTest(expression, a => a.B.C.Each());
        }

        [Test]
        public void TestAll1()
        {
            Expression<Func<A, bool>> expression = a => a.B.C.All(c => c.S == "zzz");
            DoTest(expression, a => a.B.C.Each().S);
        }

        [Test]
        public void TestAll2()
        {
            Expression<Func<A, int?>> expression = a => a.B.C.Where(c => c.D.E.All(e => e.F == "zzz")).Sum(c => c.X);
            DoTest(expression, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().X);
        }

        [Test]
        public void TestArrays()
        {
            Expression<Func<A, bool>> expression = a => a.B.C != null;
            DoTest(expression, a => a.B.C);
        }

        [Test]
        public void TestNewArray()
        {
            Expression<Func<A, int?>> expression = a => new[] {a.B.C.Sum(c => c.D.E.Sum(e => e.X)), a.X, a.B.X, a.B.C.Sum(c => c.X)}.Sum();
            DoTest(expression, a => a.B.C.Each().D.E.Each().X, a => a.X, a => a.B.X, a => a.B.C.Each().X);
        }

        [Test]
        public void TestLeafIsArray1()
        {
            Expression<Func<C, int?[]>> expression = c => c.D.Z;
            DoTest(expression, c => c.D.Z);
        }

        [Test]
        public void TestLeafIsArray2()
        {
            Expression<Func<C, int?>> expression = c => c.D.Z.Sum();
            DoTest(expression, c => c.D.Z.Each());
        }

        [Test]
        public void TestLeafIsArray3()
        {
            Expression<Func<A, int?>> expression = a => a.B.C.Sum(c => c.D.Z.Sum());
            DoTest(expression, a => a.B.C.Each().D.Z.Each());
        }

        [Test]
        public void TestNewAnonymuosObjectCreation()
        {
            Expression<Func<A, object>> expression = a => new {a.S, sum = a.B.C.Where(c => c.S == "zzz").Sum(c => c.X)};
            DoTest(expression, a => a.S, a => a.B.C.Each().S, a => a.B.C.Each().X);
        }

        [Test]
        public void TestSelect1()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select(c => c.X);
            DoTest(expression, a => a.B.C.Each().X);
        }

        [Test]
        public void TestSelectNull()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select<C, object>(c => null);
            DoTest(expression);
        }

        [Test]
        public void TestSelect2()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select(c => c.D.E.First(e => e.F == "zzz").X);
            DoTest(expression, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestSelect3()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select(c => c.D.E.FirstOrDefault(e => e.F == "zzz")).Select(e => e.X);
            DoTest(expression, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestSelect4()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select(c => c.D).Select(d => d.X);
            DoTest(expression, a => a.B.C.Each().D.X);
        }

        [Test]
        public void TestSelect5()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select(c => c.D).First(d => d.S == "zzz").X;
            DoTest(expression, a => a.B.C.Each().D.S, a => a.B.C.Each().D.X);
        }

        [Test]
        public void TestSelect6()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select(c => new {c.D.Z, c.D.E.First(e => e.F == "zzz").X}).Sum(o => o.X);
            DoTest(expression, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestSelect7()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select(c => new {c.D, e = c.D.E.First(e => e.F == "zzz")}).First(z => z.e.Z == "qxx").D.S;
            DoTest(expression, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().Z, a => a.B.C.Each().D.S);
        }

        [Test]
        public void TestSelect8()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Select(c => new {s = c.D.X + c.X, x = c.D.E.First(e => e.F == "zzz").X}).Sum(o => o.s);
            DoTest(expression, a => a.B.C.Each().D.X, a => a.B.C.Each().X, a => a.B.C.Each().D.E.Each().F);
        }

        [Test]
        public void TestSelect9()
        {
            Expression<Func<A, object>> expression = a => (from c in a.B.C
                                                           let e = c.D.E.First(e => e.F == "zzz")
                                                           select e.X).Sum();
            DoTest(expression, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestSelect10()
        {
            Expression<Func<A, object>> expression = a => (from c in a.B.C
                                                           where c.D.S == "zzz"
                                                           select c.D.X).Sum();
            DoTest(expression, a => a.B.C.Each().D.S, a => a.B.C.Each().D.X);
        }

        [Test]
        public void TestSelect11()
        {
            Expression<Func<A, object>> expression = a => (from c in a.B.C
                                                           let s = c.X + c.D.X
                                                           select s).Sum();
            DoTest(expression, a => a.B.C.Each().D.X, a => a.B.C.Each().X);
        }

        [Test]
        public void TestSelect12()
        {
            Expression<Func<A, E[]>> expression = a => a.B.C.Where(c => c.X > 0).Select(c => c.D.E).FirstOrDefault();
            DoTest(expression, a => a.B.C.Each().X, a => a.B.C.Each().D.E);
        }

        [Test]
        public void TestSelect13()
        {
            Expression<Func<A, E>> expression = a => a.B.C.FirstOrDefault(c => c.X > 0).D.E.FirstOrDefault();
            DoTest(expression, a => a.B.C.Each().X, a => a.B.C.Each().D.E.Each());
        }

        [Test]
        public void TestSelectWithTuple1()
        {
            Expression<Func<A, string>> expression = a => a.B.C.Select(c => new Tuple<D, int?>(c.D, c.X)).FirstOrDefault(tuple => tuple.Item2 > 0).Item1.S;
            DoTest(expression, a => a.B.C.Each().D.S);
        }

        [Test]
        public void TestSelectWithTuple2()
        {
            Expression<Func<A, Tuple<D, int?>>> expression = a => a.B.C.Select(c => new Tuple<D, int?>(c.D, c.X)).FirstOrDefault(tuple => tuple.Item2 > 0);
            DoTest(expression, a => a.B.C.Each().D, a => a.B.C.Each().X);
        }

        [Test]
        public void TestAggregate1()
        {
            Expression<Func<A, int?>> expression = a => a.B.C.Aggregate(a.X, (x, c) => x + c.X);
            DoTest(expression, a => a.X, a => a.B.C.Each().X);
        }

        [Test]
        public void TestSelectManyWithTuple1()
        {
            Expression<Func<A, string>> expression = a => a.B.C.SelectMany(c => c.D.E, (c, e) => new Tuple<int?, string>(c.X, e.F)).FirstOrDefault(tuple => tuple.Item1 > 0).Item2;
            DoTest(expression, a => a.B.C.Each().D.E.Each().F);
        }

        [Test]
        public void TestSelectManyWithTuple2()
        {
            Expression<Func<A, Tuple<int?, string>>> expression = a => a.B.C.SelectMany(c => c.D.E, (c, e) => new Tuple<int?, string>(c.X, e.F)).FirstOrDefault(tuple => tuple.Item1 > 0);
            DoTest(expression, a => a.B.C.Each().X, a => a.B.C.Each().D.E.Each().F);
        }

        [Test]
        public void TestSelectManyWithTuple3()
        {
            Expression<Func<A, string>> expression = a => a.B.C.SelectMany(c => c.D.E, (c, e) => new Tuple<int?, string>(c.S == "zzz" ? c.X : null, e.F)).FirstOrDefault(tuple => tuple.Item1 > 0).Item2;
            DoTest(expression, a => a.B.C.Each().S, a => a.B.C.Each().D.E.Each().F);
        }

        [Test]
        public void TestSelectMany1()
        {
            Expression<Func<A, object>> expression = a => a.B.C.Where(c => c.D.S == "zzz").SelectMany(c => c.D.E).Where(e => e.F == "qxx").Sum(e => e.X);
            DoTest(expression, a => a.B.C.Each().D.S, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestSelectMany2()
        {
            Expression<Func<A, object>> expression = a => (a.B.C.Where(c => c.D.S == "zzz").SelectMany(c => c.D.E, (c, e) => new {c, e}).Where(@t => @t.e.F == "qxx").Select(@t => @t.e.X)).Sum();
            DoTest(expression, a => a.B.C.Each().D.S, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestSelectMany3()
        {
            Expression<Func<A, object>> expression = a => (from c in a.B.C
                                                           where c.D.S == "zzz"
                                                           from e in c.D.E
                                                           where e.F == "qxx"
                                                           select e.X).Sum();
            DoTest(expression, a => a.B.C.Each().D.S, a => a.B.C.Each().D.E.Each().F, a => a.B.C.Each().D.E.Each().X);
        }

        [Test]
        public void TestStringLength()
        {
            Expression<Func<A, int>> expression = a => a.S.Length;
            DoTest(expression, a => a.S);
        }

        private static Expression ClearConverts(Expression node)
        {
            while(node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
                node = ((UnaryExpression)node).Operand;
            return node;
        }

        private static string ExpressionToString(LambdaExpression lambda)
        {
            return ExpressionCompiler.DebugViewGetter(Expression.Lambda(Expression.Convert(ClearConverts(lambda.Body), typeof(object)), lambda.Parameters));
        }

        private static void DoTest<T1, T2>(Expression<Func<T1, T2>> expression, params Expression<Func<T1, object>>[] expectedDependencies)
        {
            var actualDependencies = expression.ExtractDependencies();
            CollectionAssert.AreEquivalent(expectedDependencies.Select(ExpressionToString), actualDependencies.Select(ExpressionToString));
        }

        private static void DoTest<T1, T2, T3>(Expression<Func<T1, T2, T3>> expression, params Expression<Func<T1, T2, object>>[] expectedDependencies)
        {
            var actualDependencies = expression.ExtractDependencies();
            CollectionAssert.AreEquivalent(expectedDependencies.Select(ExpressionToString), actualDependencies.Select(ExpressionToString));
        }

        private class A
        {
            public B B { get; set; }
            public string S { get; set; }
            public int? X { get; set; }
        }

        private class B
        {
            public C[] C { get; set; }
            public IEnumerable<C> Cz { get; set; }
            public string S { get; set; }
            public int? X { get; set; }
        }

        private class C
        {
            public D D { get; set; }
            public string S { get; set; }
            public int? X { get; set; }
        }

        private class D
        {
            public E[] E { get; set; }
            public string S { get; set; }
            public int? X { get; set; }
            public int?[] Z { get; set; }
        }

        private class E
        {
            public string F { get; set; }
            public string Z { get; set; }
            public int? X { get; set; }
        }
    }
}