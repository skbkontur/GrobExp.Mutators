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
            unchecked
            {
                Assert.AreEqual(2000000000 + 2000000000, f(2000000000, 2000000000));
            }
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
            unchecked
            {
                Assert.AreEqual(2000000000 + 2000000000, f(2000000000, 2000000000));
            }
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
        public void TestAdd4()
        {
            ParameterExpression a = Expression.Parameter(typeof(int));
            ParameterExpression b = Expression.Parameter(typeof(int));
            Expression<Func<int, int, int>> exp = Expression.Lambda<Func<int, int, int>>(Expression.AddChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3, f(1, 2));
            Assert.AreEqual(1, f(-1, 2));
            Assert.Throws<OverflowException>(() => f(2000000000, 2000000000));
        }

        [Test]
        public void TestAdd5()
        {
            ParameterExpression a = Expression.Parameter(typeof(int?));
            ParameterExpression b = Expression.Parameter(typeof(int?));
            Expression<Func<int?, int?, int?>> exp = Expression.Lambda<Func<int?, int?, int?>>(Expression.AddChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3, f(1, 2));
            Assert.AreEqual(1, f(-1, 2));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
            Assert.Throws<OverflowException>(() => f(2000000000, 2000000000));
        }

        [Test]
        public void TestAdd6()
        {
            ParameterExpression a = Expression.Parameter(typeof(uint));
            ParameterExpression b = Expression.Parameter(typeof(uint));
            Expression<Func<uint, uint, uint>> exp = Expression.Lambda<Func<uint, uint, uint>>(Expression.AddChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3, f(1, 2));
            Assert.AreEqual(3000000000, f(1000000000, 2000000000));
            Assert.Throws<OverflowException>(() => f(3000000000, 2000000000));
        }

        [Test]
        public void TestAdd7()
        {
            ParameterExpression a = Expression.Parameter(typeof(uint?));
            ParameterExpression b = Expression.Parameter(typeof(uint?));
            Expression<Func<uint?, uint?, uint?>> exp = Expression.Lambda<Func<uint?, uint?, uint?>>(Expression.AddChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3, f(1, 2));
            Assert.AreEqual(3000000000, f(1000000000, 2000000000));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
            Assert.Throws<OverflowException>(() => f(3000000000, 2000000000));
        }

        [Test]
        public void TestAdd8()
        {
            Expression<Func<int?, int, int?>> exp = (a, b) => a + b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3, f(1, 2));
            Assert.AreEqual(1, f(-1, 2));
            Assert.IsNull(f(null, 2));
            unchecked
            {
                Assert.AreEqual(2000000000 + 2000000000, f(2000000000, 2000000000));
            }
        }

        [Test]
        public void TestSub1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a - b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(-1, f(1, 2));
            Assert.AreEqual(1, f(-1, -2));
            unchecked
            {
                Assert.AreEqual(2000000000 - -2000000000, f(2000000000, -2000000000));
            }
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
            unchecked
            {
                Assert.AreEqual(2000000000 - -2000000000, f(2000000000, -2000000000));
            }
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
        public void TestSub4()
        {
            ParameterExpression a = Expression.Parameter(typeof(int));
            ParameterExpression b = Expression.Parameter(typeof(int));
            Expression<Func<int, int, int>> exp = Expression.Lambda<Func<int, int, int>>(Expression.SubtractChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(-1, f(1, 2));
            Assert.AreEqual(1, f(-1, -2));
            Assert.Throws<OverflowException>(() => f(2000000000, -2000000000));
        }

        [Test]
        public void TestSub5()
        {
            ParameterExpression a = Expression.Parameter(typeof(int?));
            ParameterExpression b = Expression.Parameter(typeof(int?));
            Expression<Func<int?, int?, int?>> exp = Expression.Lambda<Func<int?, int?, int?>>(Expression.SubtractChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(-1, f(1, 2));
            Assert.AreEqual(1, f(-1, -2));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
            Assert.Throws<OverflowException>(() => f(2000000000, -2000000000));
        }

        [Test]
        public void TestSub6()
        {
            ParameterExpression a = Expression.Parameter(typeof(uint));
            ParameterExpression b = Expression.Parameter(typeof(uint));
            Expression<Func<uint, uint, uint>> exp = Expression.Lambda<Func<uint, uint, uint>>(Expression.SubtractChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3000000000, f(4000000000, 1000000000));
            Assert.Throws<OverflowException>(() => f(1, 2));
        }

        [Test]
        public void TestSub7()
        {
            ParameterExpression a = Expression.Parameter(typeof(uint?));
            ParameterExpression b = Expression.Parameter(typeof(uint?));
            Expression<Func<uint?, uint?, uint?>> exp = Expression.Lambda<Func<uint?, uint?, uint?>>(Expression.SubtractChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(3000000000, f(4000000000, 1000000000));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
            Assert.Throws<OverflowException>(() => f(1, 2));
        }

        [Test]
        public void TestSub8()
        {
            Expression<Func<int?, int, int?>> exp = (a, b) => a - b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(-1, f(1, 2));
            Assert.AreEqual(1, f(-1, -2));
            Assert.IsNull(f(null, 2));
            unchecked
            {
                Assert.AreEqual(2000000000 - -2000000000, f(2000000000, -2000000000));
            }
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
            unchecked
            {
                Assert.AreEqual(2000000000 * 2000000000, f(2000000000, 2000000000));
            }
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
            unchecked
            {
                Assert.AreEqual(2000000000 * 2000000000, f(2000000000, 2000000000));
            }
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
        public void TestMul4()
        {
            ParameterExpression a = Expression.Parameter(typeof(int));
            ParameterExpression b = Expression.Parameter(typeof(int));
            Expression<Func<int, int, int>> exp = Expression.Lambda<Func<int, int, int>>(Expression.MultiplyChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 1));
            Assert.AreEqual(2, f(1, 2));
            Assert.AreEqual(6, f(-2, -3));
            Assert.AreEqual(-20, f(-2, 10));
            Assert.Throws<OverflowException>(() => f(2000000000, 2000000000));
        }

        [Test]
        public void TestMul5()
        {
            ParameterExpression a = Expression.Parameter(typeof(int?));
            ParameterExpression b = Expression.Parameter(typeof(int?));
            Expression<Func<int?, int?, int?>> exp = Expression.Lambda<Func<int?, int?, int?>>(Expression.MultiplyChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(2, f(1, 2));
            Assert.AreEqual(6, f(-2, -3));
            Assert.AreEqual(-20, f(-2, 10));
            Assert.AreEqual(-2000000000, f(200000000, -10));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
            Assert.Throws<OverflowException>(() => f(2000000000, 2000000000));
        }

        [Test]
        public void TestMul6()
        {
            ParameterExpression a = Expression.Parameter(typeof(uint));
            ParameterExpression b = Expression.Parameter(typeof(uint));
            Expression<Func<uint, uint, uint>> exp = Expression.Lambda<Func<uint, uint, uint>>(Expression.MultiplyChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 1));
            Assert.AreEqual(2, f(1, 2));
            Assert.AreEqual(4000000000, f(2000000000, 2));
            Assert.Throws<OverflowException>(() => f(2000000000, 2000000000));
        }

        [Test]
        public void TestMul7()
        {
            ParameterExpression a = Expression.Parameter(typeof(uint?));
            ParameterExpression b = Expression.Parameter(typeof(uint?));
            Expression<Func<uint?, uint?, uint?>> exp = Expression.Lambda<Func<uint?, uint?, uint?>>(Expression.MultiplyChecked(a, b), a, b);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(2, f(1, 2));
            Assert.AreEqual(4000000000, f(2000000000, 2));
            Assert.IsNull(f(null, 2));
            Assert.IsNull(f(1, null));
            Assert.IsNull(f(null, null));
            Assert.Throws<OverflowException>(() => f(2000000000, 2000000000));
        }

        [Test]
        public void TestMul8()
        {
            Expression<Func<int?, int, int?>> exp = (a, b) => a * b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(2, f(1, 2));
            Assert.AreEqual(6, f(-2, -3));
            Assert.AreEqual(-20, f(-2, 10));
            Assert.IsNull(f(null, 2));
            unchecked
            {
                Assert.AreEqual(2000000000 * 2000000000, f(2000000000, 2000000000));
            }
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
        public void TestDiv6()
        {
            Expression<Func<int?, int, int?>> exp = (a, b) => a / b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(1, 2));
            Assert.AreEqual(2, f(5, 2));
            Assert.AreEqual(-1, f(-3, 2));
            Assert.IsNull(f(null, 2));
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

        [Test]
        public void TestModulo5()
        {
            Expression<Func<int?, int, int?>> exp = (a, b) => a % b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(1, f(1, 2));
            Assert.AreEqual(2, f(5, 3));
            Assert.AreEqual(-1, f(-3, 2));
            Assert.IsNull(f(null, 2));
        }

        [Test]
        public void TestLeftShift1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a << b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(0, f(0, 10));
            Assert.AreEqual(1024, f(1, 10));
            Assert.AreEqual(2468, f(1234, 1));
            Assert.AreEqual(16, f(1, 100));
            Assert.AreEqual(-2468, f(-1234, 1));
        }

        [Test]
        public void TestLeftShift2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a << b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(0, f(0, 10));
            Assert.AreEqual(1024, f(1, 10));
            Assert.AreEqual(2468, f(1234, 1));
            Assert.AreEqual(16, f(1, 100));
            Assert.AreEqual(-2468, f(-1234, 1));
            Assert.IsNull(f(null, 1));
            Assert.IsNull(f(123, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestLeftShift3()
        {
            Expression<Func<int?, int, int?>> exp = (a, b) => a << b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(0, f(0, 10));
            Assert.AreEqual(1024, f(1, 10));
            Assert.AreEqual(2468, f(1234, 1));
            Assert.AreEqual(16, f(1, 100));
            Assert.AreEqual(-2468, f(-1234, 1));
            Assert.IsNull(f(null, 1));
        }

        [Test]
        public void TestRightShift1()
        {
            Expression<Func<int, int, int>> exp = (a, b) => a >> b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(0, f(0, 10));
            Assert.AreEqual(1, f(1024, 10));
            Assert.AreEqual(0, f(1023, 10));
            Assert.AreEqual(1, f(3, 1));
            Assert.AreEqual(-2, f(-3, 1));
        }

        [Test]
        public void TestRightShift2()
        {
            Expression<Func<int?, int?, int?>> exp = (a, b) => a >> b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(0, f(0, 10));
            Assert.AreEqual(1, f(1024, 10));
            Assert.AreEqual(0, f(1023, 10));
            Assert.AreEqual(1, f(3, 1));
            Assert.AreEqual(-2, f(-3, 1));
            Assert.IsNull(f(null, 1));
            Assert.IsNull(f(123, null));
            Assert.IsNull(f(null, null));
        }

        [Test]
        public void TestRightShift3()
        {
            Expression<Func<uint, int, uint>> exp = (a, b) => a >> b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(0, f(0, 10));
            Assert.AreEqual(1, f(1024, 10));
            Assert.AreEqual(0, f(1023, 10));
            Assert.AreEqual(1, f(3, 1));
            Assert.AreEqual(2000000000, f(4000000000, 1));
        }

        [Test]
        public void TestRightShift4()
        {
            Expression<Func<uint?, int, uint?>> exp = (a, b) => a >> b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(0, f(0, 10));
            Assert.AreEqual(1, f(1024, 10));
            Assert.AreEqual(0, f(1023, 10));
            Assert.AreEqual(1, f(3, 1));
            Assert.AreEqual(2000000000, f(4000000000, 1));
            Assert.IsNull(f(null, 1));
        }

        [Test]
        public void TestRightShift5()
        {
            Expression<Func<uint?, int?, uint?>> exp = (a, b) => a >> b;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0, 0));
            Assert.AreEqual(0, f(0, 10));
            Assert.AreEqual(1, f(1024, 10));
            Assert.AreEqual(0, f(1023, 10));
            Assert.AreEqual(1, f(3, 1));
            Assert.AreEqual(2000000000, f(4000000000, 1));
            Assert.IsNull(f(null, 1));
            Assert.IsNull(f(123, null));
            Assert.IsNull(f(null, null));
        }

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