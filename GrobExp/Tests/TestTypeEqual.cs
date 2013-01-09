using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestTypeEqual
    {
        [Test]
        public void Test1()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, bool>>(Expression.TypeEqual(parameter, typeof(double)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(5));
        }

        [Test]
        public void Test2()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, bool>>(Expression.TypeEqual(parameter, typeof(int)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(5));
        }

        [Test]
        public void Test3()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, bool>>(Expression.TypeEqual(parameter, typeof(object)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(5));
        }

        [Test]
        public void Test4()
        {
            var parameter = Expression.Parameter(typeof(TestClassB));
            var exp = Expression.Lambda<Func<TestClassB, bool>>(Expression.TypeEqual(parameter, typeof(TestClassA)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(new TestClassB()));
        }

        [Test]
        public void Test5()
        {
            var parameter = Expression.Parameter(typeof(TestClassA));
            var exp = Expression.Lambda<Func<TestClassA, bool>>(Expression.TypeEqual(parameter, typeof(TestClassB)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsFalse(f(new TestClassA()));
            Assert.IsTrue(f(new TestClassB()));
        }

        [Test]
        public void Test6()
        {
            var parameter = Expression.Parameter(typeof(object));
            var exp = Expression.Lambda<Func<object, bool>>(Expression.TypeEqual(parameter, typeof(int)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsTrue(f(5));
            Assert.IsFalse(f(5.5));
        }

        private class TestClassA
        {
            
        }

        private class TestClassB: TestClassA
        {
            
        }
    }
}