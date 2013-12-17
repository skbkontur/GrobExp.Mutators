using System;
using System.Linq.Expressions;

using NUnit.Framework;

namespace Mutators.Tests
{
//    public class CurrentsResolverTest : TestBase
//    {
//        [Test]
//        public void TestResolveCurrents1()
//        {
//            Expression<Func<A, string>> abstractPath = a => a.B.C.Each().S;
//            Expression<Func<X, string>> exp = x => x.Y.Current().S;
//            var resolved = new CurrentsResolver(abstractPath.Body).Visit(exp.Body);
//            Expression<Func<A, X, string>> expected = (a, x) => x.Y[a.B.C.Each().CurrentIndex()].S;
//            resolved.AssertEqualsExpression(expected.Body);
//        }
//
//        [Test]
//        public void TestResolveCurrents2()
//        {
//            Expression<Func<A, string>> abstractPath = a => a.B.C.Each().D.E.Each().Z;
//            Expression<Func<X, string>> exp = x => x.Y.Current().S;
//            var resolved = new CurrentsResolver(abstractPath.Body).Visit(exp.Body);
//            Expression<Func<A, X, string>> expected = (a, x) => x.Y[a.B.C.Each().CurrentIndex()].S;
//            resolved.AssertEqualsExpression(expected.Body);
//        }
//
//        [Test]
//        public void TestResolveCurrents3()
//        {
//            Expression<Func<A, string>> abstractPath = a => a.B.C.Each().D.E.Each().Z;
//            Expression<Func<X, string>> exp = x => x.Y.Current().Z.Current().S;
//            var resolved = new CurrentsResolver(abstractPath.Body).Visit(exp.Body);
//            Expression<Func<A, X, string>> expected = (a, x) => x.Y[a.B.C.Each().CurrentIndex()].Z[a.B.C.Each().D.E.Each().CurrentIndex()].S;
//            resolved.AssertEqualsExpression(expected.Body);
//        }
//
//        [Test]
//        public void TestResolveCurrentIndex1()
//        {
//            Expression<Func<A, string>> abstractPath = a => a.B.C.Each().S;
//            Expression<Func<X, int>> exp = x => x.Y.Current().CurrentIndex();
//            var resolved = new CurrentsResolver(abstractPath.Body).Visit(exp.Body);
//            Expression<Func<A, X, int>> expected = (a, x) => a.B.C.Each().CurrentIndex();
//            resolved.AssertEqualsExpression(expected.Body);
//        }
//
//        [Test]
//        public void TestResolveCurrentIndex2()
//        {
//            Expression<Func<A, string>> abstractPath = a => a.B.C.Each().D.E.Each().Z;
//            Expression<Func<X, int>> exp = x => x.Y.Current().Z.Current().CurrentIndex();
//            var resolved = new CurrentsResolver(abstractPath.Body).Visit(exp.Body);
//            Expression<Func<A, X, int>> expected = (a, x) => a.B.C.Each().D.E.Each().CurrentIndex();
//            resolved.AssertEqualsExpression(expected.Body);
//        }
//
//        private class A
//        {
//            public B B { get; set; }
//            public string S { get; set; }
//            public int? X { get; set; }
//        }
//
//        private class B
//        {
//            public C[] C { get; set; }
//            public string S { get; set; }
//            public int? X { get; set; }
//        }
//
//        private class C
//        {
//            public D D { get; set; }
//            public string S { get; set; }
//            public int? X { get; set; }
//        }
//
//        private class D
//        {
//            public E[] E { get; set; }
//            public string S { get; set; }
//            public int? X { get; set; }
//            public int?[] Z { get; set; }
//        }
//
//        private class E
//        {
//            public string F { get; set; }
//            public string Z { get; set; }
//            public int? X { get; set; }
//        }
//
//        private class X
//        {
//            public Y[] Y { get; set; }
//            public string S { get; set; }
//        }
//
//        private class Y
//        {
//            public Z[] Z { get; set; }
//            public string S { get; set; }
//        }
//
//        private class Z
//        {
//            public string S { get; set; }
//        }
//    }
}