using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestCustomOperators
    {
        [Test]
        public void TestAdd()
        {
            Expression<Func<Fraction, Fraction, Fraction>> exp = (a, b) => a + b;
            var f = LambdaCompiler.Compile(exp);
            Assert.That(new Fraction(5, 6) == f(new Fraction(1, 2), new Fraction(1, 3)));
            ParameterExpression parameterA = Expression.Parameter(typeof(Fraction));
            ParameterExpression parameterB = Expression.Parameter(typeof(Fraction));
            Expression<Func<Fraction, Fraction, Fraction>> exp2 = Expression.Lambda<Func<Fraction, Fraction, Fraction>>(Expression.AddChecked(parameterA, parameterB, ((BinaryExpression)exp.Body).Method), parameterA, parameterB);
            f = LambdaCompiler.Compile(exp2);
            Assert.That(new Fraction(5, 6) == f(new Fraction(1, 2), new Fraction(1, 3)));
        }

        [Test]
        public void TestSubtract()
        {
            Expression<Func<Fraction, Fraction, Fraction>> exp = (a, b) => a - b;
            var f = LambdaCompiler.Compile(exp);
            Assert.That(new Fraction(1, 6) == f(new Fraction(1, 2), new Fraction(1, 3)));
            ParameterExpression parameterA = Expression.Parameter(typeof(Fraction));
            ParameterExpression parameterB = Expression.Parameter(typeof(Fraction));
            Expression<Func<Fraction, Fraction, Fraction>> exp2 = Expression.Lambda<Func<Fraction, Fraction, Fraction>>(Expression.SubtractChecked(parameterA, parameterB, ((BinaryExpression)exp.Body).Method), parameterA, parameterB);
            f = LambdaCompiler.Compile(exp2);
            Assert.That(new Fraction(1, 6) == f(new Fraction(1, 2), new Fraction(1, 3)));
        }

        [Test]
        public void TestMultiply()
        {
            Expression<Func<Fraction, Fraction, Fraction>> exp = (a, b) => a * b;
            var f = LambdaCompiler.Compile(exp);
            Assert.That(new Fraction(1, 6) == f(new Fraction(1, 2), new Fraction(1, 3)));
            ParameterExpression parameterA = Expression.Parameter(typeof(Fraction));
            ParameterExpression parameterB = Expression.Parameter(typeof(Fraction));
            Expression<Func<Fraction, Fraction, Fraction>> exp2 = Expression.Lambda<Func<Fraction, Fraction, Fraction>>(Expression.MultiplyChecked(parameterA, parameterB, ((BinaryExpression)exp.Body).Method), parameterA, parameterB);
            f = LambdaCompiler.Compile(exp2);
            Assert.That(new Fraction(1, 6) == f(new Fraction(1, 2), new Fraction(1, 3)));
        }

        [Test]
        public void TestDivide()
        {
            Expression<Func<Fraction, Fraction, Fraction>> exp = (a, b) => a / b;
            var f = LambdaCompiler.Compile(exp);
            Assert.That(new Fraction(3, 2) == f(new Fraction(1, 2), new Fraction(1, 3)));
        }

        [Test]
        public void TestUnaryPlus()
        {
            Expression<Func<Fraction, Fraction>> exp = a => +a;
            var f = LambdaCompiler.Compile(exp);
            Assert.That(new Fraction(1, 3) == f(new Fraction(1, 3)));
        }

        [Test]
        public void TestUnaryMinus()
        {
            Expression<Func<Fraction, Fraction>> exp = a => -a;
            var f = LambdaCompiler.Compile(exp);
            Assert.That(new Fraction(-1, 3) == f(new Fraction(1, 3)));
            ParameterExpression parameter = Expression.Parameter(typeof(Fraction));
            Expression<Func<Fraction, Fraction>> exp2 = Expression.Lambda<Func<Fraction, Fraction>>(Expression.NegateChecked(parameter, ((UnaryExpression)exp.Body).Method), parameter);
            f = LambdaCompiler.Compile(exp2);
            Assert.That(new Fraction(-1, 3) == f(new Fraction(1, 3)));
        }

        [Test]
        public void TestEqual()
        {
            Expression<Func<Fraction, Fraction, bool>> exp = (a, b) => a == b;
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(new Fraction(1, 2), new Fraction(1, 2)));
            Assert.IsFalse(f(new Fraction(1, 3), new Fraction(1, 2)));
        }

        [Test]
        public void TestNotEqual()
        {
            Expression<Func<Fraction, Fraction, bool>> exp = (a, b) => a != b;
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(new Fraction(1, 2), new Fraction(1, 2)));
            Assert.IsTrue(f(new Fraction(1, 3), new Fraction(1, 2)));
        }

        [Test]
        public void TestGreaterThan()
        {
            Expression<Func<Fraction, Fraction, bool>> exp = (a, b) => a > b;
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(new Fraction(1, 2), new Fraction(1, 2)));
            Assert.IsTrue(f(new Fraction(1, 2), new Fraction(1, 3)));
            Assert.IsFalse(f(new Fraction(1, 3), new Fraction(1, 2)));
        }

        [Test]
        public void TestLessThan()
        {
            Expression<Func<Fraction, Fraction, bool>> exp = (a, b) => a < b;
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(new Fraction(1, 2), new Fraction(1, 2)));
            Assert.IsFalse(f(new Fraction(1, 2), new Fraction(1, 3)));
            Assert.IsTrue(f(new Fraction(1, 3), new Fraction(1, 2)));
        }

        [Test]
        public void TestGreaterThanOrEqual()
        {
            Expression<Func<Fraction, Fraction, bool>> exp = (a, b) => a >= b;
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(new Fraction(1, 2), new Fraction(1, 2)));
            Assert.IsTrue(f(new Fraction(1, 2), new Fraction(1, 3)));
            Assert.IsFalse(f(new Fraction(1, 3), new Fraction(1, 2)));
        }

        [Test]
        public void TestLessThanOrEqual()
        {
            Expression<Func<Fraction, Fraction, bool>> exp = (a, b) => a <= b;
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(new Fraction(1, 2), new Fraction(1, 2)));
            Assert.IsFalse(f(new Fraction(1, 2), new Fraction(1, 3)));
            Assert.IsTrue(f(new Fraction(1, 3), new Fraction(1, 2)));
        }

        private class Fraction
        {
            public Fraction(int num, int den)
            {
                Num = num;
                Den = den;
                Normalize();
            }

            public static Fraction operator +(Fraction fraction)
            {
                return fraction == null ? null : new Fraction(fraction.Num, fraction.Den);
            }

            public static Fraction operator -(Fraction fraction)
            {
                return fraction == null ? null : new Fraction(-fraction.Num, fraction.Den);
            }

            public static Fraction operator +(Fraction left, Fraction right)
            {
                if(left == null || right == null)
                    return null;
                return new Fraction(left.Num * right.Den + left.Den * right.Num, left.Den * right.Den);
            }

            public static Fraction operator -(Fraction left, Fraction right)
            {
                if(left == null || right == null)
                    return null;
                return new Fraction(left.Num * right.Den - left.Den * right.Num, left.Den * right.Den);
            }

            public static Fraction operator *(Fraction left, Fraction right)
            {
                if(left == null || right == null)
                    return null;
                return new Fraction(left.Num * right.Num, left.Den * right.Den);
            }

            public static Fraction operator /(Fraction left, Fraction right)
            {
                if(left == null || right == null)
                    return null;
                return new Fraction(left.Num * right.Den, left.Den * right.Num);
            }

            public static bool operator ==(Fraction left, Fraction right)
            {
                if(ReferenceEquals(left, null) || ReferenceEquals(right, null))
                    return ReferenceEquals(left, null) && ReferenceEquals(right, null);
                return left.Num == right.Num && left.Den == right.Den;
            }

            public static bool operator !=(Fraction left, Fraction right)
            {
                return !(left == right);
            }

            public static bool operator <(Fraction left, Fraction right)
            {
                if(left == null || right == null)
                    return false;
                return left.Num * right.Den < left.Den * right.Num;
            }

            public static bool operator >(Fraction left, Fraction right)
            {
                if(left == null || right == null)
                    return false;
                return left.Num * right.Den > left.Den * right.Num;
            }

            public static bool operator <=(Fraction left, Fraction right)
            {
                if(left == null || right == null)
                    return false;
                return left.Num * right.Den <= left.Den * right.Num;
            }

            public static bool operator >=(Fraction left, Fraction right)
            {
                if(left == null || right == null)
                    return false;
                return left.Num * right.Den >= left.Den * right.Num;
            }

            public int Num { get; private set; }
            public int Den { get; private set; }

            private void Normalize()
            {
                var gcd = Gcd(Num, Den);
                Num /= gcd;
                Den /= gcd;
                if(Den < 0)
                {
                    Num = -Num;
                    Den = -Den;
                }
            }

            private static int Gcd(int a, int b)
            {
                while(b != 0)
                {
                    var r = a % b;
                    a = b;
                    b = r;
                }
                return a;
            }
        }
    }
}