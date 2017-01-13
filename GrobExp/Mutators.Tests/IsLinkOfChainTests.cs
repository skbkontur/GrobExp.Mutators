using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class IsLinkOfChainTests : TestBase
    {
        private T Identity<T>(T x)
        {
            return x;
        }

        [Test]
        public void Test()
        {
            TestTrue(qxx => qxx.Data);
            TestTrue(qxx => qxx.Data.Where(x => !string.IsNullOrEmpty(x)));
            TestTrue(qxx => qxx.Data.Where(x => !string.IsNullOrEmpty(x)).Current());
            TestTrue(qxx => qxx.Data.Where(x => !string.IsNullOrEmpty(x)).ToList().FirstOrDefault().Each(), recursive : true);

            TestFalse(qxx => Identity(qxx));
            TestFalse(qxx => qxx.GetHashCode());

            TestTrue(qxx => Identity(qxx).Data);
            TestFalse(qxx => Identity(qxx).Data, recursive : true);
            TestTrue(qxx => qxx.Data.Select(x => Identity(x) ?? ""), recursive : true);

            TestTrue(qxx => 1);
            TestFalse(qxx => 1, restrictConstants : true);
            TestTrue(qxx => qxx.Data[1], restrictConstants : true);
            TestFalse(qxx => qxx.Data.FirstOrDefault() + "GRobas", restrictConstants : true);
            TestTrue(qxx => (qxx.Data.FirstOrDefault() ?? "Grobas").Length, restrictConstants : true);
            TestFalse(qxx => (qxx.Data.FirstOrDefault() ?? "Grobas").Length, restrictConstants : true, recursive : true);
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

        private interface IQxx
        {
            string[] Data { get; }
        }
    }
}