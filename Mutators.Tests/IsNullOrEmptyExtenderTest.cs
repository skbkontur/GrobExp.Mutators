using System;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class IsNullOrEmptyExtenderTest : TestBase
    {
        [Test]
        public void TestStringEqualsToNull()
        {
            Expression<Func<string, bool>> exp = s => s == null;
            var extended = Extend(exp).Compile();
            Assert.IsTrue(extended(null));
            Assert.IsTrue(extended(""));
            Assert.IsFalse(extended("zzz"));
        }

        [Test]
        public void TestStringNotEqualsToNull()
        {
            Expression<Func<string, bool>> exp = s => s != null;
            var extended = Extend(exp).Compile();
            Assert.IsFalse(extended(null));
            Assert.IsFalse(extended(""));
            Assert.IsTrue(extended("zzz"));
        }

        [Test]
        public void TestArrayEqualsToNull()
        {
            Expression<Func<int[], bool>> exp = ints => ints == null;
            var extended = Extend(exp).Compile();
            Assert.IsTrue(extended(null));
            Assert.IsTrue(extended(new int[0]));
            Assert.IsFalse(extended(new[] {0}));
        }

        [Test]
        public void TestArrayNotEqualsToNull()
        {
            Expression<Func<int[], bool>> exp = ints => ints != null;
            var extended = Extend(exp).Compile();
            Assert.IsFalse(extended(null));
            Assert.IsFalse(extended(new int[0]));
            Assert.IsTrue(extended(new[] {0}));
        }

        [Test]
        public void TestStringArrayEqualsToNull()
        {
            Expression<Func<string[], bool>> exp = strings => strings == null;
            var extended = Extend(exp).Compile();
            Assert.IsTrue(extended(null));
            Assert.IsTrue(extended(new string[0]));
            Assert.IsTrue(extended(new string[] {null}));
            Assert.IsTrue(extended(new[] {""}));
            Assert.IsTrue(extended(new[] {null, ""}));
            Assert.IsFalse(extended(new[] {null, "zzz"}));
            Assert.IsFalse(extended(new[] {"zzz", null}));
        }

        [Test]
        public void TestStringArrayNotEqualsToNull()
        {
            Expression<Func<string[], bool>> exp = strings => strings != null;
            var extended = Extend(exp).Compile();
            Assert.IsFalse(extended(null));
            Assert.IsFalse(extended(new string[0]));
            Assert.IsFalse(extended(new string[] {null}));
            Assert.IsFalse(extended(new[] {""}));
            Assert.IsFalse(extended(new[] {null, ""}));
            Assert.IsTrue(extended(new[] {null, "zzz"}));
            Assert.IsTrue(extended(new[] {"zzz", null}));
        }

        private static Expression<TDelegate> Extend<TDelegate>(Expression<TDelegate> exp)
        {
            return (Expression<TDelegate>)new IsNullOrEmptyExtender().Visit(exp);
        }
    }
}