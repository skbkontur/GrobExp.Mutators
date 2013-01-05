using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests.ArithmeticTests
{
    [TestFixture]
    public class TestBitwiseOperations
    {
        [Test]
        public void TestAnd1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a & b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 123));
            Assert.AreEqual(1, f(3, 5));
            Assert.AreEqual(17235476 & 73172563, f(17235476, 73172563));
        }

        [Test]
        public void TestAnd2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a & b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 123));
            Assert.AreEqual(1, f(3, 5));
            Assert.AreEqual(17235476 & 73172563, f(17235476, 73172563));
            Assert.IsNull(f(null, 1));
            Assert.IsNull(f(123, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestAnd3()
        {
            Expression<Func<int, int?, int?>> exp = (a, b) => a & b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 123));
            Assert.AreEqual(1, f(3, 5));
            Assert.AreEqual(17235476 & 73172563, f(17235476, 73172563));
            Assert.IsNull(f(123, null));
        }

        [Test]
        public void TestAnd4()
        {
            Expression<Func<long, long, long>> exp = (a, b) => a & b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 123));
            Assert.AreEqual(1, f(3, 5));
            Assert.AreEqual(172354712312316 & 73123123172563, f(172354712312316, 73123123172563));
        }

        [Test]
        public void TestOr1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a | b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(7, f(3, 5));
            Assert.AreEqual(17235476 | 73172563, f(17235476, 73172563));
        }

        [Test]
        public void TestOr2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a | b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(7, f(3, 5));
            Assert.AreEqual(17235476 | 73172563, f(17235476, 73172563));
            Assert.IsNull(f(null, 1));
            Assert.IsNull(f(123, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestOr3()
        {
            Expression<Func<int, int?, int?>> exp = (a, b) => a | b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(7, f(3, 5));
            Assert.AreEqual(17235476 | 73172563, f(17235476, 73172563));
            Assert.IsNull(f(123, null));
        }

        [Test]
        public void TestOr4()
        {
            Expression<Func<long, long, long>> exp = (a, b) => a | b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(7, f(3, 5));
            Assert.AreEqual(172354712312316 | 73123123172563, f(172354712312316, 73123123172563));
        }

        [Test]
        public void TestXor1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a ^ b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(6, f(3, 5));
            Assert.AreEqual(17235476 ^ 73172563, f(17235476, 73172563));
        }

        [Test]
        public void TestXor2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a ^ b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(6, f(3, 5));
            Assert.AreEqual(17235476 ^ 73172563, f(17235476, 73172563));
            Assert.IsNull(f(null, 1));
            Assert.IsNull(f(123, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestXor3()
        {
            Expression<Func<int, int?, int?>> exp = (a, b) => a ^ b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(6, f(3, 5));
            Assert.AreEqual(17235476 ^ 73172563, f(17235476, 73172563));
            Assert.IsNull(f(123, null));
        }

        [Test]
        public void TestXor4()
        {
            Expression<Func<long, long, long>> exp = (a, b) => a ^ b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(6, f(3, 5));
            Assert.AreEqual(172354712312316 ^ 73123123172563, f(172354712312316, 73123123172563));
        }
    }
}