using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestQuote
    {
        [Test]
        public void Test1()
        {
            Expression<Func<int, int>> exp = i => F(j => j * j);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(25, f(2));
        }

        [Test]
        public void Test2()
        {
            Expression<Func<int, int>> exp = i => F(j => j * i);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(10, f(2));
        }

        [Test]
        public void Test3()
        {
            ParameterExpression x = Expression.Parameter(typeof(int));
            ParameterExpression y = Expression.Parameter(typeof(int));
            Expression body = Expression.Call(typeof(TestQuote).GetMethod("F2", BindingFlags.NonPublic | BindingFlags.Static), Expression.Quote(Expression.Lambda<Func<int, IRuntimeVariables>>(Expression.RuntimeVariables(x, y), y)));
            Expression<Func<int, int>> exp = Expression.Lambda<Func<int, int>>(body, x);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(10, f(10));
        }

        private static int F(Expression<Func<int, int>> exp)
        {
            return LambdaCompiler.Compile(exp)(5);
        }

        private static int F2(Expression<Func<int, IRuntimeVariables>> exp)
        {
            return (int)LambdaCompiler.Compile(exp)(5)[0];
        }
    }
}