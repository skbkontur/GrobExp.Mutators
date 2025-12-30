using System;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

using NUnit.Framework;
using NUnit.Framework.Legacy;

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
            ClassicAssert.IsTrue(extended(null));
            ClassicAssert.IsTrue(extended(""));
            ClassicAssert.IsFalse(extended("zzz"));
        }

        [Test]
        public void TestStringNotEqualsToNull()
        {
            Expression<Func<string, bool>> exp = s => s != null;
            var extended = Extend(exp).Compile();
            ClassicAssert.IsFalse(extended(null));
            ClassicAssert.IsFalse(extended(""));
            ClassicAssert.IsTrue(extended("zzz"));
        }

        [Test]
        public void TestArrayEqualsToNull()
        {
            Expression<Func<int[], bool>> exp = ints => ints == null;
            var extended = Extend(exp).Compile();
            ClassicAssert.IsTrue(extended(null));
            ClassicAssert.IsTrue(extended(new int[0]));
            ClassicAssert.IsFalse(extended(new[] {0}));
        }

        [Test]
        public void TestArrayNotEqualsToNull()
        {
            Expression<Func<int[], bool>> exp = ints => ints != null;
            var extended = Extend(exp).Compile();
            ClassicAssert.IsFalse(extended(null));
            ClassicAssert.IsFalse(extended(new int[0]));
            ClassicAssert.IsTrue(extended(new[] {0}));
        }

        [Test]
        public void TestStringArrayEqualsToNull()
        {
            Expression<Func<string[], bool>> exp = strings => strings == null;
            var extended = Extend(exp).Compile();
            ClassicAssert.IsTrue(extended(null));
            ClassicAssert.IsTrue(extended(new string[0]));
            ClassicAssert.IsTrue(extended(new string[] {null}));
            ClassicAssert.IsTrue(extended(new[] {""}));
            ClassicAssert.IsTrue(extended(new[] {null, ""}));
            ClassicAssert.IsFalse(extended(new[] {null, "zzz"}));
            ClassicAssert.IsFalse(extended(new[] {"zzz", null}));
        }

        [Test]
        public void TestStringArrayNotEqualsToNull()
        {
            Expression<Func<string[], bool>> exp = strings => strings != null;
            var extended = Extend(exp).Compile();
            ClassicAssert.IsFalse(extended(null));
            ClassicAssert.IsFalse(extended(new string[0]));
            ClassicAssert.IsFalse(extended(new string[] {null}));
            ClassicAssert.IsFalse(extended(new[] {""}));
            ClassicAssert.IsFalse(extended(new[] {null, ""}));
            ClassicAssert.IsTrue(extended(new[] {null, "zzz"}));
            ClassicAssert.IsTrue(extended(new[] {"zzz", null}));
        }

        private static Expression<TDelegate> Extend<TDelegate>(Expression<TDelegate> exp)
        {
            return (Expression<TDelegate>)new IsNullOrEmptyExtender().Visit(exp);
        }
    }
}