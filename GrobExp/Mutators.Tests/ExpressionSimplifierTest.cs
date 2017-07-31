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
        public void Constant()
        {
            Check((A a) => a.S == "zzz", a => a.S == "zzz");
        }

        [Test]
        public void LocalVarValue()
        {
            string s = "zzz";
            Check((A a) => a.S == s, a => a.S == "zzz");
        }

        [Test]
        public void LocalVarFieldString()
        {
            var b = new B {S = "zzz"};
            Check((A a) => a.S == b.S, a => a.S == "zzz");
        }

        [Test]
        public void LocalVarFieldEnum()
        {
            var x = new A {E = E.Two};
            Check((A a) => a.E == x.E, "a => (a.E == Two)");
        }

        [Test]
        public void LocalVarFieldNullableEnum()
        {
            var x = new A {E2 = E.Two};
            Check((A a) => a.E2 == x.E2, "a => (a.E2 == Two)");
        }

        [Test(Description = "Since constants are compiled with All compiler options on, accessing a field of a 'null' object yields null")]
        public void LocalVarNullField()
        {
            B b = null;
            Check((A a) => a.S == b.S, a => a.S == null);
        }

        [Test]
        public void IfNotNullInlineValue()
        {
            var b = new B {S = "zzz"};
            Check((A a) => a.S == b.S.IfNotNull(), a => a.S == "zzz");
        }

        [Test]
        public void IfNotNullInlineValueAndAddNullCheck()
        {
            var b = new B {S = "zzz"};
            Check((A a) => a.S.IfNotNull() == b.S.IfNotNull(), a => a.S == null || a.S == "zzz");
        }

        [Test]
        public void IfNotNullSubstituteNull()
        {
            var b = new B();
            Check((A a) => a.S == b.S.IfNotNull(), a => true);
        }

        [Test]
        public void IfNotNullInlineNullAndAddNullCheck()
        {
            var b = new B();
            Check((A a) => a.S.IfNotNull() == b.S.IfNotNull(), a => true);
        }

        [Test]
        public void OrEliminateTrue()
        {
            int x = 1;
            Check((A a) => x == 1 || a.S == "zzz", a => true);
        }

        [Test]
        public void OrEliminateFalse()
        {
            int x = 0;
            Check((A a) => x == 1 || a.S == "zzz", a => a.S == "zzz");
        }

        [Test]
        public void AndEliminateTrue()
        {
            int x = 1;
            Check((A a) => x == 1 && a.S == "zzz", a => a.S == "zzz");
        }

        [Test]
        public void AndEliminateFalse()
        {
            int x = 0;
            Check((A a) => x == 1 && a.S == "zzz", a => false);
        }


        [Test]
        public void LeaveIntactDynamic()
        {
            Check((A a) => a.DateTime > DateTime.Now.Dynamic(), a => a.DateTime > DateTime.Now.Dynamic());
        }

        [Test]
        public void LeaveIntactDateTimeUtcNow()
        {
            Check((A a) => a.DateTime > DateTime.UtcNow, a => a.DateTime > DateTime.UtcNow);
        }

        [Test]
        public void LeaveIntactNewGuid()
        {
            Check((A a) => a.Id == Guid.NewGuid(), a => a.Id == Guid.NewGuid());
        }

        [Test]
        public void AnonymousTypeInlineField()
        {
            Check((A a) => new { s = a.S, a.B.S }.S == "zzz", a => a.B.S == "zzz");
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

        private void Check<TArg, TResult>(Expression<Func<TArg, TResult>> expression, string expectedSimplified)
        {
            var simplifiedExpression = simplifier.Simplify(expression);
            Assert.AreEqual(expectedSimplified, simplifiedExpression.ToString());
        }

        private void Check<TArg, TResult>(Expression<Func<TArg, TResult>> expression, Expression<Func<TArg, TResult>> expectedSimplified)
        {
            var simplifiedExpression = simplifier.Simplify(expression);
            Assert.True(ExpressionEquivalenceChecker.Equivalent(simplifiedExpression, expectedSimplified, strictly: false, distinguishEachAndCurrent: true),
                "Failed to simplify expression:\nExpected to get '{0}',\n        but got '{1}'", expectedSimplified, simplifiedExpression);
        }

        private ExpressionSimplifier simplifier;
    }
}