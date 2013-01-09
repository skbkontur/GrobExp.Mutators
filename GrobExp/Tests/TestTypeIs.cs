using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestTypeIs
    {
        [Test]
        public void Test1()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, bool>>(Expression.TypeIs(parameter, typeof(double)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(5));
        }

        [Test]
        public void Test2()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, bool>>(Expression.TypeIs(parameter, typeof(int)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(5));
        }

        [Test]
        public void Test3()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, bool>>(Expression.TypeIs(parameter, typeof(object)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(5));
        }

        [Test]
        public void Test4()
        {
            var parameter = Expression.Parameter(typeof(TestClassB));
            var exp = Expression.Lambda<Func<TestClassB, bool>>(Expression.TypeIs(parameter, typeof(TestClassA)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(new TestClassB()));
        }

        [Test]
        public void Test5()
        {
            var parameter = Expression.Parameter(typeof(TestClassA));
            var exp = Expression.Lambda<Func<TestClassA, bool>>(Expression.TypeIs(parameter, typeof(TestClassB)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(new TestClassA()));
            Assert.IsTrue(f(new TestClassB()));
        }

        private class TestClassA
        {
            
        }

        private class TestClassB: TestClassA
        {
            
        }
    }
}