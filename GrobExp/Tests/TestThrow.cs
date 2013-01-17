﻿using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestThrow
    {
        [Test]
        public void Test1()
        {
            Expression<Action> exp = Expression.Lambda<Action>(Expression.Throw(Expression.Constant(new Exception())));
            var f = LambdaCompiler.Compile(exp);
            Assert.Throws<Exception>(() => f());
        }

        [Test]
        public void Test2()
        {
            Expression<Action> exp = Expression.Lambda<Action>(Expression.Throw(Expression.New(typeof(Exception))));
            var f = LambdaCompiler.Compile(exp);
            Assert.Throws<Exception>(() => f());
        }
    }
}