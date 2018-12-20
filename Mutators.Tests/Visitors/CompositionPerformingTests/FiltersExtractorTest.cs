using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;
using GrobExp.Mutators.Visitors.CompositionPerforming;

using NUnit.Framework;

namespace Mutators.Tests.Visitors.CompositionPerformingTests
{
    [TestFixture]
    public class FiltersExtractorTest
    {
        [Test]
        public void MembersPath()
        {
            DoTest<A, string>(a => a.B.S,
                              a => a.B.S);
        }

        [Test]
        public void PathWithConstantIndex()
        {
            DoTest<A, string>(a => a.Bs[0].S,
                              a => a.Bs[0].S);
        }

        [Test]
        public void PathWithEach()
        {
            DoTest<A, string>(a => a.Bs.Each().S,
                              a => a.Bs.Each().S,
                              expectedFilters : new LambdaExpression[] {null});
        }

        [Test]
        public void PathWithCurrent()
        {
            DoTest<A, string>(a => a.Bs.Current().S,
                              a => a.Bs.Current().S,
                              expectedFilters : new LambdaExpression[] {null});
        }

        [Test]
        public void PathEndsWithWhere()
        {
            DoTest<A, IEnumerable<B>>(a => a.Bs.Where(b => b.N == 5),
                                      a => a.Bs,
                                      expectedFilters : (Expression<Func<A, bool>>)(a => a.Bs.Each().N == 5));
        }

        [Test]
        public void PathWithWhereFollowedByEach()
        {
            DoTest<A, string>(a => a.Bs.Where(b => b.N == 5).Each().S,
                              a => a.Bs.Each().S,
                              expectedFilters : (Expression<Func<A, bool>>)(a => a.Bs.Each().N == 5));
        }

        [Test]
        public void PathWithWhereFollowedByCurrent()
        {
            DoTest<A, string>(a => a.Bs.Where(b => b.N == 5).Current().S,
                              a => a.Bs.Current().S,
                              expectedFilters : (Expression<Func<A, bool>>)(a => a.Bs.Current().N == 5));
        }

        [Test]
        public void NotExtractFromArgumentsOfStaticMethodCall()
        {
            TestNothingHappens<A, string>(a => MyMethod(a.B, a.Bs.Where(b => b.N == 0)).S);

            TestNothingHappens<A, string>(a => MyMethod(a.Bs.Where(b => b.N == 0).Each(), a.Bs).S);
        }

        [Test]
        public void ExtractFilterFromFirstArgumentOfExtensionCall()
        {
            DoTest<A, int>(a => TestExtensions.ExtensionMethod(a.Bs.Where(b => b.S == "qxx").Each()).N,
                           a => TestExtensions.ExtensionMethod(a.Bs.Each()).N,
                           expectedFilters : (Expression<Func<A, bool>>)(a => a.Bs.Each().S == "qxx"));
        }

        [Test]
        public void NotExtractFilterFromOtherArgumentsOfExtensionCall()
        {
            TestNothingHappens<A, int>(a => TestExtensions.ExtensionMethod(a.B, a.Bs.Where(b => b.S == "qxx").Each()).N);
        }

        [Test]
        public void NotExtractFilterFromArgumentsOfInstanceMethodCall()
        {
            DoTest<A, string>(a => a.Method(a.Bs.Where(b => b.S == "qxx")),
                              a => a.Method(a.Bs.Where(b => b.S == "qxx")));
        }

        [Test]
        public void ConvertNotSupported()
        {
            TestNotSupported<A, object>(a => (object)a.S);
        }

        [Test]
        public void CoalesceNotSupported()
        {
            TestNotSupported<A, object>(a => a.S ?? "zzz");
        }

        [Test]
        public void ExpressionNotSmashingToSmithereens()
        {
            TestNothingHappens<A, string>(a => a.S + a.Bs.Where(b => b.N == 10).Each().S);
            TestNothingHappens<A, Func<string>>(a => () => a.Bs.Where(b => b.S == "www").Each().S);
        }

        private static void DoTest<T1, T2>(Expression<Func<T1, T2>> source, Expression<Func<T1, T2>> expected, params LambdaExpression[] expectedFilters)
        {
            var result = source.Body.CleanFilters(out var filters);
            Assert.That(ExpressionEquivalenceChecker.Equivalent(expected.Body, result, strictly : false, distinguishEachAndCurrent : true),
                        () => "Expected expression:\n" + expected.Body + "\nResult expression:\n" + result);
            Assert.That(filters.Length, Is.EqualTo(expectedFilters.Length),
                        () => "Expected filters:\n" +
                              string.Join("\n", expectedFilters.Select(x => x.ToString())) +
                              "\nActual filters:\n" +
                              string.Join("\n", filters.Select(x => x.ToString())));
            foreach (var (expectedFilter, filter) in expectedFilters.Zip(filters, (x, y) => (x, y)))
            {
                Assert.That(ExpressionEquivalenceChecker.Equivalent(expectedFilter?.Body, filter?.Body, strictly : false, distinguishEachAndCurrent : true),
                            () => "Expected filter:\n" + expectedFilter?.Body + "\nResult filter:\n" + filter?.Body);
            }
        }

        private static void TestNotSupported<T1, T2>(Expression<Func<T1, T2>> exp)
        {
            Assert.Throws<NotSupportedException>(() => exp.Body.CleanFilters(out _));
        }

        private static void TestNothingHappens<T1, T2>(Expression<Func<T1, T2>> source)
        {
            DoTest(source, source);
        }

        private static T MyMethod<T>(T value, IEnumerable<T> values) => value;

        private class A
        {
            public string S { get; set; }

            public B B { get; set; }

            public B[] Bs { get; set; }

            public string Method(IEnumerable<B> bs)
            {
                return S;
            }
        }

        private class B
        {
            public int N { get; set; }

            public string S { get; set; }

            public string GetS() => S;
        }
    }

    internal static class TestExtensions
    {
        public static T ExtensionMethod<T>(this T value) => value;

        public static T ExtensionMethod<T>(this T value, T anotherValue) => value;
    }
}