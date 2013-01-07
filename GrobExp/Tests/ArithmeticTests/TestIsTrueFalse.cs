using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests.ArithmeticTests
{
    [TestFixture]
    public class TestIsTrueFalse
    {
        [Test]
        public void TestIsTrue1()
        {
            Expression<Func<bool>> exp = Expression.Lambda<Func<bool>>(Expression.IsTrue(Expression.Constant(true)));
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f());
        }

        [Test]
        public void TestIsTrue2()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(int));
            Expression<Func<int, bool>> exp = Expression.Lambda<Func<int, bool>>(Expression.IsTrue(Expression.NotEqual(parameter, Expression.Constant(0))), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(10));
            Assert.IsFalse(f(0));
        }

        [Test]
        public void TestIsTrue3()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TestClassA));
            Expression<Func<TestClassA, bool>> exp = Expression.Lambda<Func<TestClassA, bool>>(Expression.IsTrue(Expression.GreaterThanOrEqual(Expression.MakeMemberAccess(parameter, typeof(TestClassA).GetProperty("X")), Expression.Constant(0, typeof(int?)))), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(null));
            Assert.IsFalse(f(new TestClassA()));
            Assert.IsFalse(f(new TestClassA{X = -1}));
            Assert.IsTrue(f(new TestClassA{X = 0}));
        }

        [Test]
        public void TestIsFalse1()
        {
            Expression<Func<bool>> exp = Expression.Lambda<Func<bool>>(Expression.IsFalse(Expression.Constant(false)));
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f());
        }

        [Test]
        public void TestIsFalse2()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(int));
            Expression<Func<int, bool>> exp = Expression.Lambda<Func<int, bool>>(Expression.IsFalse(Expression.NotEqual(parameter, Expression.Constant(0))), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(10));
            Assert.IsTrue(f(0));
        }

        [Test]
        public void TestIsFalse3()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TestClassA));
            Expression<Func<TestClassA, bool>> exp = Expression.Lambda<Func<TestClassA, bool>>(Expression.IsFalse(Expression.GreaterThanOrEqual(Expression.MakeMemberAccess(parameter, typeof(TestClassA).GetProperty("X")), Expression.Constant(0, typeof(int?)))), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(null));
            Assert.IsFalse(f(new TestClassA()));
            Assert.IsTrue(f(new TestClassA{X = -1}));
            Assert.IsFalse(f(new TestClassA{X = 0}));
        }

        private class TestClassA
        {
            public int? X { get; set; }
        }
    }
}