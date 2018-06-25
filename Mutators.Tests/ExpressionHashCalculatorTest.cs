using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class ExpressionHashCalculatorTest : TestBase
    {
        [Test]
        public void Test1()
        {
            Expression<Func<TestClassA, string>> exp1 = a => a.S;
            Expression<Func<TestClassA, string>> exp2 = aa => aa.S;
            var hash1 = ExpressionHashCalculator.CalcHashCode(exp1, true);
            var hash2 = ExpressionHashCalculator.CalcHashCode(exp2, true);
            Assert.AreNotEqual(hash1, hash2);
            hash1 = ExpressionHashCalculator.CalcHashCode(exp1, false);
            hash2 = ExpressionHashCalculator.CalcHashCode(exp2, false);
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void Test2()
        {
            Expression<Func<TestClassA, string>> exp1 = a => a.S;
            Expression<Func<TestClassB, string>> exp2 = a => a.S;
            var hash1 = ExpressionHashCalculator.CalcHashCode(exp1, true);
            var hash2 = ExpressionHashCalculator.CalcHashCode(exp2, true);
            Assert.AreNotEqual(hash1, hash2);
            hash1 = ExpressionHashCalculator.CalcHashCode(exp1, false);
            hash2 = ExpressionHashCalculator.CalcHashCode(exp2, false);
            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void Test3()
        {
            Expression<Func<TestClassA, string>> exp1 = a => a.ArrayB.First(b => b.S.Length > 0).S;
            Expression<Func<TestClassA, string>> exp2 = aa => aa.ArrayB.First(bb => bb.S.Length > 0).S;
            var hash1 = ExpressionHashCalculator.CalcHashCode(exp1, true);
            var hash2 = ExpressionHashCalculator.CalcHashCode(exp2, true);
            Assert.AreNotEqual(hash1, hash2);
            hash1 = ExpressionHashCalculator.CalcHashCode(exp1, false);
            hash2 = ExpressionHashCalculator.CalcHashCode(exp2, false);
            Assert.AreEqual(hash1, hash2);
        }

        private class TestClassA
        {
            public string S { get; set; }
            public TestClassB[] ArrayB { get; set; }
        }

        private class TestClassB
        {
            public string S { get; set; }
        }
    }
}