using System;
using System.Linq.Expressions;

using GrobExp.Mutators;

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
        public void TestMergeStupidExpression()
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

        [Test]
        public void TestMergeTwoParameters()
        {
            Expression<Func<D, E, string>> exp = (d, e) => d.E[0].F + e.Z;
            Expression<Func<A, string>> merged = exp.Merge<A, D, E, string>(a => a.B.C[1].D, a => a.B.C[13].D.E[23]);
            merged.AssertEqualsExpression(a => a.B.C[1].D.E[0].F + a.B.C[13].D.E[23].Z);
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