using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestQuote
    {
        [Test]
        public void Test()
        {
            Expression<Func<int, int>> exp = i => F(j => j * i);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(10, f(2));
        }

        private static int F(Expression<Func<int, int>> exp)
        {
            return LambdaCompiler.Compile(exp)(5);
        }
    }
}