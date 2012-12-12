using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestArithmetic
    {
        [Test]
        public void TestAdd1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a + b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3, f(1, 2));
            Assert.AreEqual(1, f(-1, 2));
        }

        [Test]
        public void TestAdd2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a + b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3, f(1, 2));
            Assert.AreEqual(1, f(-1, 2));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestAdd3()
        {
            Expression<Func<int?, long?, long?>> exp = (a, b) => a + b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3, f(1, 2));
            Assert.AreEqual(1, f(-1, 2));
            Assert.AreEqual(12000000000, f(2000000000, 10000000000));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestSub1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a - b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(-1, f(1, 2));
            Assert.AreEqual(1, f(-1, -2));
        }

        [Test]
        public void TestSub2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a - b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(-1, f(1, 2));
            Assert.AreEqual(1, f(-1, -2));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestSub3()
        {
            Expression<Func<int?, long?, long?>> exp = (a, b) => a - b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(-1, f(1, 2));
            Assert.AreEqual(1, f(-1, -2));
            Assert.AreEqual(-8000000000, f(2000000000, 10000000000));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestMul1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a * b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 1));
            Assert.AreEqual(2, f(1, 2));
            Assert.AreEqual(6, f(-2, -3));
            Assert.AreEqual(-20, f(-2, 10));
        }

        [Test]
        public void TestMul2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a * b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(2, f(1, 2));
            Assert.AreEqual(6, f(-2, -3));
            Assert.AreEqual(-20, f(-2, 10));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestMul3()
        {
            Expression<Func<int?, long?, long?>> exp = (a, b) => a * b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(2, f(1, 2));
            Assert.AreEqual(6, f(-2, -3));
            Assert.AreEqual(-20, f(-2, 10));
            Assert.AreEqual(-20000000000, f(2000000000, -10));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestDiv1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a / b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(1, 2));
            Assert.AreEqual(2, f(5, 2));
            Assert.AreEqual(-1, f(-3, 2));
        }

        [Test]
        public void TestDiv2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a / b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(1, 2));
            Assert.AreEqual(2, f(5, 2));
            Assert.AreEqual(-1, f(-3, 2));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestDiv3()
        {
            Expression<Func<int?, long?, long?>> exp = (a, b) => a / b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(1, 2));
            Assert.AreEqual(2, f(5, 2));
            Assert.AreEqual(-1, f(-3, 2));
            Assert.AreEqual(0, f(2000000000, 20000000000));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestDiv4()
        {
            Expression<Func<double, double, double>> exp = (a, b) => a / b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0.5, f(1, 2));
            Assert.AreEqual(2.5, f(5, 2));
            Assert.AreEqual(-1.5, f(-3, 2));
        }

        [Test]
        public void TestDiv5()
        {
            Expression<Func<uint, uint, uint>> exp = (a, b) => a / b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(1, 2));
            Assert.AreEqual(2, f(5, 2));
            Assert.AreEqual(2147483646, f(uint.MaxValue - 3 + 1, 2));
        }

        [Test]
        public void TestModulo1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a % b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(1, 2));
            Assert.AreEqual(2, f(5, 3));
            Assert.AreEqual(-1, f(-3, 2));
        }

        [Test]
        public void TestModulo2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a % b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(1, 2));
            Assert.AreEqual(2, f(5, 3));
            Assert.AreEqual(-1, f(-3, 2));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestModulo3()
        {
            Expression<Func<int?, long?, long?>> exp = (a, b) => a % b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(1, 2));
            Assert.AreEqual(2, f(5, 3));
            Assert.AreEqual(-1, f(-3, 2));
            Assert.AreEqual(2000000000, f(2000000000, 20000000000));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestModulo4()
        {
            Expression<Func<uint, uint, uint>> exp = (a, b) => a % b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(1, 2));
            Assert.AreEqual(2, f(5, 3));
            Assert.AreEqual(1, f(uint.MaxValue - 3 + 1, 2));
        }
    }
}