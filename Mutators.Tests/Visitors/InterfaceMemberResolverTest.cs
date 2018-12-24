using System;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests.Visitors
{
    [TestFixture]
    public class InterfaceMemberResolverTest
    {
        [Test]
        public void ResolveImplementationCastedToInterfaceRemovesCast()
        {
            Expression<Func<A, string>> source = a => ((IInterfaceB)a.B).S;

            var resolved = interfaceMemberResolver.Visit(source.Body);
            Expression<Func<A, string>> expected = a => a.B.S;
            DoTest(resolved, expected.Body);
        }

        [Test]
        public void ResolveInterfaceCastedToInterfaceRemovesCast()
        {
            Expression<Func<A, string>> source = a => ((IInterfaceB)a.InterfaceB).S;

            var resolved = interfaceMemberResolver.Visit(source.Body);
            Expression<Func<A, string>> expected = a => a.InterfaceB.S;
            DoTest(resolved, expected.Body);
        }

        [Test]
        public void TwoPropertiesCastedInterfaces()
        {
            Expression<Func<A, string>> source = a => ((IInterfaceB)((IInterfaceA)a).B).S;

            var resolved = interfaceMemberResolver.Visit(source.Body);
            Expression<Func<A, string>> expected = a => a.B.S;
            DoTest(resolved, expected.Body);
        }

        [Test]
        public void ResolveInterfacePropertyReturnsTheSameExpression()
        {
            Expression<Func<A, string>> source = a => a.InterfaceB.S;

            var expression = interfaceMemberResolver.Visit(source.Body);
            DoTest(expression, source.Body);
        }

        [Test]
        public void ResolvePropertyCastedToInterfaceMethodDoesNothing()
        {
            Expression<Func<A, string>> source = a => ((IInterfaceB)a.B).Method();

            var expression = interfaceMemberResolver.Visit(source.Body);
            DoTest(expression, source.Body);
        }

        private static void DoTest(Expression expression, Expression expected)
        {
            Assert.That(ExpressionEquivalenceChecker.Equivalent(expression, expected, strictly : false, distinguishEachAndCurrent : true),
                        () => $"Expected:\n{expected}\nBut was:\n{expression}");
        }

        private interface IInterfaceA
        {
            B B { get; }
        }

        private interface IInterfaceB
        {
            string S { get; }

            string Method();
        }

        private static readonly InterfaceMemberResolver interfaceMemberResolver = new InterfaceMemberResolver();

        private class A : IInterfaceA
        {
            public B B { get; set; }

            public IInterfaceB InterfaceB { get; set; }
        }

        private class B : IInterfaceB
        {
            public string S { get; set; }

            public string Method()
            {
                return string.Empty;
            }
        }
    }
}