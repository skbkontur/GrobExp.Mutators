using System;
using System.Linq.Expressions;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class AbstractPathResolverTest : TestBase
    {
        [Test]
        public void TestResolveAbstractPath1()
        {
            Expression<Func<A, string>> path = a => a.B.C[13].D.E[10].F;
            Expression<Func<A, D>> abstractPath = a => a.B.C.Current().D;
            var resolved = ExpressionExtensions.ResolveAbstractPath(path, abstractPath);
            Expression<Func<A, D>> expected = a => a.B.C[13].D;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPath2()
        {
            Expression<Func<A, string>> path = a => a.B.C[13].D.E[10].F;
            Expression<Func<A, string>> abstractPath = a => a.B.C.Current().D.E.Current().F;
            var resolved = ExpressionExtensions.ResolveAbstractPath(path, abstractPath);
            Expression<Func<A, string>> expected = a => a.B.C[13].D.E[10].F;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPath3()
        {
            Expression<Func<A, string>> path = a => a.B.C[13].D.E[10].F;
            Expression<Func<A, string>> abstractPath = a => a.B.C.Current().D.E[13].Z;
            var resolved = ExpressionExtensions.ResolveAbstractPath(path, abstractPath);
            Expression<Func<A, string>> expected = a => a.B.C[13].D.E[13].Z;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPath4()
        {
            Expression<Func<A, string>> path = a => a.B.C[13].D.E[10].F;
            Expression<Func<A, string>> abstractPath = a => a.B.C[13].D.E.Current().Z;
            var resolved = ExpressionExtensions.ResolveAbstractPath(path, abstractPath);
            Expression<Func<A, string>> expected = a => a.B.C[13].D.E[10].Z;
            resolved.AssertEqualsExpression(expected);
        }

        [Test]
        public void TestResolveAbstractPathTemplateIndex()
        {
            Expression<Func<A, string>> path = a => a.B.C.TemplateIndex().D.E.TemplateIndex().F;
            Expression<Func<A, D>> abstractPath = a => a.B.C.Current().D;
            var resolved = ExpressionExtensions.ResolveAbstractPath(path, abstractPath);
            Expression<Func<A, D>> expected = a => a.B.C.TemplateIndex().D;
            resolved.AssertEqualsExpression(expected);
        }
//
//        [Test]
//        public void TestResolveAbstractPathWithSubPaths1()
//        {
//            Expression<Func<A, B>> subPath1 = a => a.B;
//            Expression<Func<B, C>> subPath2 = b => b.C.Each();
//            var parameters = new List<PathPrefix>
//                {
//                    new PathPrefix(subPath1.Body, subPath2.Parameters[0]),
//                };
//            Expression<Func<A, string>> exp = a => a.S;
//            var resolved = exp.Body.ResolveAbstractPath(parameters);
//            resolved.AssertEqualsExpression(exp.Body);
//        }
//
//        [Test]
//        public void TestResolveAbstractPathWithSubPaths2()
//        {
//            Expression<Func<A, B>> subPath1 = a => a.B;
//            Expression<Func<B, C>> subPath2 = b => b.C.Each();
//            var parameters = new List<PathPrefix>
//                {
//                    new PathPrefix(subPath1.Body, subPath2.Parameters[0]),
//                };
//            Expression<Func<A, B>> exp = a => a.B;
//            var resolved = exp.Body.ResolveAbstractPath(parameters);
//            resolved.AssertEqualsExpression(subPath2.Parameters[0]);
//        }
//
//        [Test]
//        public void TestResolveAbstractPathWithSubPaths3()
//        {
//            Expression<Func<A, B>> subPath1 = a => a.B;
//            Expression<Func<B, C>> subPath2 = b => b.C.Each();
//            var parameters = new List<PathPrefix>
//                {
//                    new PathPrefix(subPath1.Body, subPath2.Parameters[0]),
//                };
//            Expression<Func<A, string>> exp = a => a.B.S;
//            var resolved = exp.Body.ResolveAbstractPath(parameters);
//            resolved.AssertEqualsExpression(((Expression<Func<B, string>>)(b => b.S)).Body);
//        }
//
//        [Test]
//        public void TestResolveAbstractPathWithSubPaths4()
//        {
//            Expression<Func<A, B>> subPath1 = a => a.B;
//            Expression<Func<B, C>> subPath2 = b => b.C.Each();
//            Expression<Func<C, E>> subPath3 = c => c.D.E.Each();
//            var parameters = new List<PathPrefix>
//                {
//                    new PathPrefix(subPath1.Body, subPath2.Parameters[0]),
//                    new PathPrefix(subPath2.Body, subPath3.Parameters[0]),
//                };
//            Expression<Func<A, C>> exp = a => a.B.C.Each();
//            var resolved = exp.Body.ResolveAbstractPath(parameters);
//            resolved.AssertEqualsExpression(subPath3.Parameters[0]);
//        }
//
//        [Test]
//        public void TestResolveAbstractPathWithSubPaths5()
//        {
//            Expression<Func<A, B>> subPath1 = a => a.B;
//            Expression<Func<B, C>> subPath2 = b => b.C.Each();
//            Expression<Func<C, E>> subPath3 = c => c.D.E.Each();
//            var parameters = new List<PathPrefix>
//                {
//                    new PathPrefix(subPath1.Body, subPath2.Parameters[0]),
//                    new PathPrefix(subPath2.Body, subPath3.Parameters[0]),
//                };
//            Expression<Func<A, D>> exp = a => a.B.C.Each().D;
//            var resolved = exp.Body.ResolveAbstractPath(parameters);
//            resolved.AssertEqualsExpression(((Expression<Func<C, D>>)(c => c.D)).Body);
//        }
//
//        [Test]
//        public void TestResolveAbstractPathWithSubPaths6()
//        {
//            Expression<Func<A, B>> subPath1 = a => a.B;
//            Expression<Func<B, C>> subPath2 = b => b.C.Each();
//            Expression<Func<C, E>> subPath3 = c => c.D.E.Each();
//            Expression<Func<E, string>> subPath4 = e => e.F;
//            var parameters = new List<PathPrefix>
//                {
//                    new PathPrefix(subPath1.Body, subPath2.Parameters[0]),
//                    new PathPrefix(subPath2.Body, subPath3.Parameters[0]),
//                    new PathPrefix(subPath3.Body, subPath4.Parameters[0]),
//                };
//            Expression<Func<A, string>> exp = a => a.B.C.Each().D.E.Each().F;
//            var resolved = exp.Body.ResolveAbstractPath(parameters);
//            resolved.AssertEqualsExpression(((Expression<Func<E, string>>)(e => e.F)).Body);
//        }

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