using System;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class ExpressionMergerTests : TestBase
    {
        [Test]
        public void TestMergeSimple()
        {
            Expression<Func<A, string>> merged = ExpressionExtensions.Merge<A, D, string>(a => a.B.C[1].D, d => d.E[0].F);
            merged.AssertEqualsExpression(a => a.B.C[1].D.E[0].F);
        }

        [Test]
        public void TestMergeToTrivialExpression()
        {
            Expression<Func<A, D>> merged = ExpressionExtensions.Merge<A, D, D>(a => a.B.C[1].D, d => d);
            merged.AssertEqualsExpression(a => a.B.C[1].D);
        }

        [Test]
        public void TestMergeGetElementIndex()
        {
            Expression<Func<A, C>> merged = ExpressionExtensions.Merge<A, C[], C>(a => a.B.C, c => c[89]);
            merged.AssertEqualsExpression(a => a.B.C[89]);
        }

        [Test]
        public void TestMergeNotChain()
        {
            Expression<Func<A, string>> merged = ExpressionExtensions.Merge<A, D, string>(a => a.B.C[1].D, d => d.E[0].F + d.E[10].Z);
            merged.AssertEqualsExpression(a => a.B.C[1].D.E[0].F + a.B.C[1].D.E[10].Z);
        }

        [Test]
        public void TestMergeTemplateIndex()
        {
            Expression<Func<A, C>> merged = ExpressionExtensions.Merge<A, C[], C>(a => a.B.C, c => c.TemplateIndex());
            merged.AssertEqualsExpression(a => a.B.C.TemplateIndex());

            Expression<Func<A, int?>> merged1 = ExpressionExtensions.Merge<A, C, int?>(a => a.B.C.TemplateIndex(), c => c.D.E.TemplateIndex().X);
            merged1.AssertEqualsExpression(a => a.B.C.TemplateIndex().D.E.TemplateIndex().X);
        }

        [Test(Description = "А как правильно должно работать: объединять параметры одного типа в один или нет?")]
        public void TestMergeTwoParameters()
        {
            Expression<Func<D, E, string>> exp = (d, e) => d.E[0].F + e.Z;
            Expression<Func<A, string>> merged = exp.Merge<A, D, E, string>(a => a.B.C[1].D, a => a.B.C[13].D.E[23]);
            Expression<Func<A, string>> expected = a => a.B.C[1].D.E[0].F + a.B.C[13].D.E[23].Z;
            merged.Body.AssertEqualsExpression(expected.Body);
            Assert.That(merged.Parameters.Count, Is.Not.EqualTo(expected.Parameters.Count));
        }

        [Test]
        public void TestMergeWithEach()
        {
            Expression<Func<A, string>> merged = ExpressionExtensions.Merge<A, D, string>(a => a.B.C.Each().D, d => d.E.Each().F);
            merged.AssertEqualsExpression(a => a.B.C.Each().D.E.Each().F);
        }

        [Test]
        public void TestMergeThreeParameters()
        {
            Expression<Func<D, E, B, string>> exp = (d, e, b) => d.E[0].F + e.Z + b.S;
            Expression<Func<A, string>> merged = exp.Merge<A, D, E, B, string>(a => a.B.C[1].D, a => a.B.C[13].D.E[23], a => a.B);
            Expression<Func<A, string>> expected = a => a.B.C[1].D.E[0].F + a.B.C[13].D.E[23].Z + a.B.S;
            merged.Body.AssertEqualsExpression(expected.Body);
        }

        [Test]
        public void TestMergeFrom2Roots()
        {
            Expression<Func<A, string>> pathFromRoot1 = a => a.S;
            Expression<Func<B, D>> pathFromRoot2 = b => b.C[1].D;

            Expression<Func<string, D, string>> exp = (s, d) => s + d.S;

            var merged = exp.MergeFrom2Roots(pathFromRoot1, pathFromRoot2);
            Expression<Func<A, B, string>> expected = (a, b) => a.S + b.C[1].D.S;
            merged.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestReplaceParameter()
        {
            Expression<Func<B, string>> exp = b => b.C.Each().D.E[2].F;
            Expression<Func<B, string>> expWithReplacedParameter = ExpressionExtensions.Merge<B, B, string>(b => b, exp);
            expWithReplacedParameter.AssertEqualsExpression(exp);
            Assert.That(expWithReplacedParameter.Parameters[0], Is.Not.EqualTo(exp.Parameters[0]));
        }

        [Test]
        public void TestThrowsOnNullExpression()
        {
            Expression<Func<B, string>> exp = b => b.S;
            Assert.Throws<ArgumentNullException>(() => exp.Merge(null));
        }

        [Test]
        public void TestThrowsWhenPathsCountGreaterThanExpressionParametersCount()
        {
            Expression<Func<A, string>> pathFromRoot1 = a => a.S;
            Expression<Func<B, D>> pathFromRoot2 = b => b.C[1].D;
            Expression<Func<D, string>> exp = d => d.S;
            var merger = new ExpressionMerger(pathFromRoot1, pathFromRoot2);
            Assert.Throws<ArgumentException>(() => merger.Merge(exp));
        }

        [Test]
        public void TestThrowsWhenPathsCountLessThanExpressionParametersCount()
        {
            Expression<Func<A, string>> path = a => a.S;
            Expression<Func<A, D, string>> exp = (a, d) => a.S;
            Assert.Throws<ArgumentException>(() => path.Merge(exp));
        }

        [Test]
        public void TestPathOfTypeNotEqualToTypeOfExpressionParameter()
        {
            Expression<Func<A, string>> path = a => a.S;
            Expression<Func<D, string>> exp = d => d.S;
            Assert.Throws<InvalidOperationException>(() => path.Merge(exp));
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