using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class IsLinkOfChainTests : TestBase
    {
        [Test]
        public void TestParameter()
        {
            TestTrue(qxx => qxx, recursive : true);
        }

        [Test]
        public void TestMember()
        {
            TestTrue(qxx => qxx.Int, recursive : true);
        }

        [Test]
        public void TestStringLength()
        {
            TestTrue(x => x.String.Length, recursive : true);
        }

        [Test]
        public void TestUnknownMethod()
        {
            TestFalse(qxx => Identity(qxx), recursive : true);
        }

        [Test]
        public void TestMemberAfterUnknownMethod()
        {
            TestTrue(qxx => Identity(qxx).Int, recursive : false);
            TestFalse(qxx => Identity(qxx).Int, recursive : true);
        }

        [Test]
        public void TestObjectMethod()
        {
            TestFalse(qxx => qxx.GetHashCode(), recursive : false);
        }

        [Test]
        public void TestLinqMethods()
        {
            TestTrue(qxx => qxx.Array.Select(x => x), recursive : true);
            TestTrue(qxx => qxx.Array.Where(x => true), recursive : true);
            TestTrue(qxx => qxx.Array.ToArray(), recursive : true);
        }

        [Test]
        public void TestUnknownMethodInLinqMethod()
        {
            TestTrue(qxx => qxx.Array.Select(x => Identity(x)), recursive : true);
        }

        [Test]
        public void TestMutatorsMethods()
        {
            TestTrue(qxx => qxx.Array.Each());
            TestTrue(qxx => qxx.Array.Current());
            TestTrue(qxx => qxx.Array.Current().CurrentIndex());
            TestTrue(qxx => qxx.Array.Dynamic());
            TestTrue(qxx => qxx.Array);
        }

        [Test]
        public void TestLinqAndMutatorsMethods()
        {
            TestTrue(qxx => qxx.Array.Where(x => !string.IsNullOrEmpty(x)).ToList().FirstOrDefault().Each(), recursive : true);
        }

        [Test]
        public void TestRestrictConstants()
        {
            TestTrue(qxx => 1, restrictConstants : false);
            TestFalse(qxx => 1, restrictConstants : true);
        }

        [Test]
        public void TestToString()
        {
            TestFalse(qxx => qxx.ToString());
        }

        [Test]
        public void TestUnaryExpressions()
        {
            TestFalse(x => -x.Int, recursive : false);
            TestFalse(x => x.Int as int?, recursive : false);
            TestFalse(x => x.Array.Length, recursive : false);
        }

        [Test]
        public void TestConvert()
        {
            TestTrue(x => (long)x.Int, recursive : true);
        }

        [Test]
        public void TestBinaryExpressions()
        {
            TestFalse(qxx => qxx.Array[qxx.Int] + qxx.Array[qxx.Int], recursive : false);
            TestFalse(qxx => qxx.Int * qxx.Int, recursive : false);
            TestFalse(qxx => qxx.Array.FirstOrDefault() ?? "Grobas", restrictConstants : false, recursive : false);
        }

        [Test]
        public void TestArrayIndex()
        {
            TestTrue(qxx => qxx.Array[qxx.Int], recursive : true);
        }

        [Test]
        public void TestArrayConstantIndex()
        {
            TestTrue(qxx => qxx.Array[1], restrictConstants : true, recursive : true);
        }

        [Test]
        public void TestDictionaryGetItem()
        {
            TestTrue(qxx => qxx.Dictionary["abc"], restrictConstants : false, recursive : false);
        }

        [Test]
        public void TestDictionaryOtherMethods()
        {
            TestFalse(qxx => qxx.Dictionary.ContainsKey("abc"), restrictConstants : false, recursive : false);
            TestFalse(qxx => qxx.Dictionary.Remove("abc"), restrictConstants : false, recursive : false);
        }

        [Test]
        public void TestArrayIndexer()
        {
            TestTrue(qxx => qxx.Array.GetValue(0), restrictConstants : false, recursive : true);
        }

        private void TestTrue<T>(Expression<Func<IQxx, T>> expression, bool recursive = false, bool restrictConstants = false)
        {
            DoTest(expression, recursive, restrictConstants, true);
        }

        private void TestFalse<T>(Expression<Func<IQxx, T>> expression, bool recursive = false, bool restrictConstants = false)
        {
            DoTest(expression, recursive, restrictConstants, false);
        }

        private static void DoTest<T>(Expression<Func<IQxx, T>> expression, bool recursive, bool restrictConstants, bool result)
        {
            Assert.That(expression.Body.IsLinkOfChain(restrictConstants, recursive), Is.EqualTo(result),
                        "Expected that {0} is {1}link of chain with recursive:{2} and restrictConstants:{3}",
                        expression.Body, result ? "" : "not ", recursive, restrictConstants);
        }

        private T Identity<T>(T x)
        {
            return x;
        }

        private interface IQxx
        {
            string[] Array { get; }

            int Int { get; }

            string String { get; }

            Dictionary<string, string> Dictionary { get; }
        }
    }
}