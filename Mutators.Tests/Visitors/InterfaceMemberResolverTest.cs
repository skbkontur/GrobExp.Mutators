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
        public void ResolvePropertyCastedToInterfaceCallRemovesCast()
        {
            Expression<Func<A, string>> exp = a => ((IInterface)a.B).S;

            var expression = interfaceMemberResolver.Visit(exp.Body);
            Expression<Func<A, string>> expected = a => a.B.S;
            DoTest(expression, expected.Body);
        }

        [Test]
        public void ResolveInterfacePropertyReturnsTheSameExpression()
        {
            Expression<Func<A, string>> exp = a => a.Interface.S;

            var expression = interfaceMemberResolver.Visit(exp.Body);
            DoTest(expression, exp.Body);
        }

        [Test]
        public void ResolvePropertyCastedToInterfaceMethodDoesNothing()
        {
            Expression<Action<A>> exp = a => ((IInterface)a.B).Method();

            var expression = interfaceMemberResolver.Visit(exp.Body);
            DoTest(expression, exp.Body);
        }

        private static void DoTest(Expression expression, Expression expected)
        {
            Assert.That(ExpressionEquivalenceChecker.Equivalent(expression, expected, strictly : false, distinguishEachAndCurrent : true),
                        () => $"Expected:\n{expected}\nBut was:\n{expression}");
        }

        private interface IInterface
        {
            string S { get; }

            void Method();
        }

        private static readonly InterfaceMemberResolver interfaceMemberResolver = new InterfaceMemberResolver();

        private class A
        {
            public B B { get; set; }

            public IInterface Interface { get; set; }
        }

        private class B : IInterface
        {
            public string S { get; set; }

            public void Method()
            {
            }
        }
    }
}