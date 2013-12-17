using System;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class ExpressionSimplifierTest : TestBase
    {
        protected override void SetUp()
        {
            base.SetUp();
            simplifier = new ExpressionSimplifier();
        }

        [Test]
        public void TestConstant()
        {
            Expression<Func<A, bool>> exp = a => a.S == "zzz";
            DoTest(exp.Body, "(a.S == \"zzz\")");
        }

        [Test]
        public void TestVariable1()
        {
            string s = "zzz";
            Expression<Func<A, bool>> exp = a => a.S == s;
            DoTest(exp.Body, "(a.S == \"zzz\")");
        }

        [Test]
        public void TestVariable2()
        {
            var b = new B {S = "zzz"};
            Expression<Func<A, bool>> exp = a => a.S == b.S;
            DoTest(exp.Body, "(a.S == \"zzz\")");
        }

        [Test]
        public void TestEnum()
        {
            var x = new A {E = E.Two};
            Expression<Func<A, bool>> exp = a => a.E == x.E;
            DoTest(exp.Body, "(a.E == Two)");

        }

        [Test]
        public void TestNullableEnum()
        {
            var x = new A {E2 = E.Two};
            Expression<Func<A, bool>> exp = a => a.E2 == x.E2;
            DoTest(exp.Body, "(a.E2 == Two)");

        }

        [Test]
        public void TestVariableNull()
        {
            B b = null;
            Expression<Func<A, bool>> exp = a => a.S == b.S;
            DoTest(exp.Body, "(a.S == null)");
        }

        [Test]
        public void TestIfNotNull1()
        {
            var b = new B {S = "zzz"};
            Expression<Func<A, bool>> exp = a => a.S == b.S.IfNotNull();
            DoTest(exp.Body, "(a.S == \"zzz\")");
        }

        [Test]
        public void TestIfNotNull2()
        {
            var b = new B();
            Expression<Func<A, bool>> exp = a => a.S == b.S.IfNotNull();
            DoTest(exp.Body, "True");
        }

        [Test]
        public void TestIfNotNull3()
        {
            var b = new B {S = "zzz"};
            Expression<Func<A, bool>> exp = a => a.S.IfNotNull() == b.S.IfNotNull();
            DoTest(exp.Body, "((a.S == null) OrElse (a.S == \"zzz\"))");
        }

        [Test]
        public void TestIfNotNull4()
        {
            var b = new B();
            Expression<Func<A, bool>> exp = a => a.S.IfNotNull() == b.S.IfNotNull();
            DoTest(exp.Body, "True");
        }

        [Test]
        public void TestEliminateTrueInOr()
        {
            int x = 1;
            Expression<Func<A, bool>> exp = a => x == 1 || a.S == "zzz";
            DoTest(exp.Body, "True");
        }

        [Test]
        public void TestEliminateFalseInOr()
        {
            int x = 0;
            Expression<Func<A, bool>> exp = a => x == 1 || a.S == "zzz";
            DoTest(exp.Body, "(a.S == \"zzz\")");
        }

        [Test]
        public void TestEliminateTrueInAnd()
        {
            int x = 1;
            Expression<Func<A, bool>> exp = a => x == 1 && a.S == "zzz";
            DoTest(exp.Body, "(a.S == \"zzz\")");
        }

        [Test]
        public void TestEliminateFalseInAnd()
        {
            int x = 0;
            Expression<Func<A, bool>> exp = a => x == 1 && a.S == "zzz";
            DoTest(exp.Body, "False");
        }

        [Test]
        public void TestDateTime()
        {
            Expression<Func<A, bool>> exp = a => a.DateTime > DateTime.UtcNow;
            var simplifiedExp = simplifier.Simplify(exp);
            Assert.AreEqual(ExpressionCompiler.DebugViewGetter(exp), ExpressionCompiler.DebugViewGetter(simplifiedExp));
        }

        [Test]
        public void TestNewGuid()
        {
            Expression<Func<A, bool>> exp = a => a.Id == Guid.NewGuid();
            var simplifiedExp = simplifier.Simplify(exp);
            Assert.AreEqual(ExpressionCompiler.DebugViewGetter(exp), ExpressionCompiler.DebugViewGetter(simplifiedExp));
        }

        [Test]
        public void TestAnonymousType()
        {
            Expression<Func<A, bool>> exp = a => new{s = a.S, a.B.S}.S == "zzz";
            var simplifiedExp = simplifier.Simplify(exp);
            Expression<Func<A, bool>> expectedExp = a => a.B.S == "zzz";
            Assert.AreEqual(ExpressionCompiler.DebugViewGetter(expectedExp), ExpressionCompiler.DebugViewGetter(simplifiedExp));
        }

        public class A
        {
            public string S { get; set; }
            public B B { get; set; }
            public DateTime? DateTime { get; set; }
            public Guid? Id { get; set; }
            public E E { get; set; }
            public E? E2 { get; set; }
        }

        public class B
        {
            public string S { get; set; }
        }

        public enum E
        {
            Zero,
            One,
            Two
        }

        private void DoTest(Expression exp, string expected)
        {
            var simplifiedExp = simplifier.Simplify(exp);
            Assert.AreEqual(expected, simplifiedExp.ToString());
        }

        private ExpressionSimplifier simplifier;
    }
}