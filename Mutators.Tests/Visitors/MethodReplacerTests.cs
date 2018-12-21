using System;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using JetBrains.Annotations;

using NUnit.Framework;

namespace Mutators.Tests.Visitors
{
    [TestFixture]
    public class MethodReplacerTests
    {
        [Test]
        public void NoMethods()
        {
            Expression<Func<A, string>> source = a => a.S + a.B.S;
            DoTest(source.ReplaceMethod(firstMethodInfo, secondMethodInfo), source);
        }

        [Test]
        public void ReplaceNonGenericStaticMethod()
        {
            Expression<Func<A, string>> source = a => FirstMethod(a.S);
            Expression<Func<A, string>> replaced = a => SecondMethod(a.S);

            DoTest(source.ReplaceMethod(firstMethodInfo, secondMethodInfo), replaced);
        }

        [Test]
        public void ReplaceNonGenericInstanceMethod()
        {
            Expression<Func<A, string>> source = a => a.FirstMethod(a.S);
            Expression<Func<A, string>> replaced = a => a.SecondMethod(a.S);

            DoTest(source.ReplaceMethod(A.FirstMethodInfo, A.SecondMethodInfo), replaced);
        }

        [Test]
        public void ReplaceGenericStaticMethod()
        {
            Expression<Func<A, string>> source = a => FirstMethod<string>(a.S);
            Expression<Func<A, string>> replaced = a => SecondMethod<string>(a.S);

            DoTest(source.ReplaceMethod(firstGenericMethodInfo, secondGenericMethodInfo), replaced);
        }

        [Test]
        public void ReplaceConstructedGenericStaticMethod()
        {
            Expression<Func<A, string>> source = a => FirstMethod<string>(a.S);
            Expression<Func<A, string>> replaced = a => SecondMethod<string>(a.S);

            DoTest(source.ReplaceMethod(firstConstructedGenericMethodInfo, secondConstructedGenericMethodInfo), replaced);
        }

        [Test]
        public void DistinguishGenericAndNonGenericMethods()
        {
            Expression<Func<A, string>> source = a => FirstMethod<string>(a.S);
            DoTest(source.ReplaceMethod(firstMethodInfo, secondMethodInfo), source);

            source = a => FirstMethod(a.S);
            DoTest(source.ReplaceMethod(firstGenericMethodInfo, secondGenericMethodInfo), source);
        }

        [Test]
        public void ReplaceGenericByNonGenericMethodThrows()
        {
            Expression<Func<A, string>> source = a => FirstMethod<string>(a.S);
            Assert.Throws<InvalidOperationException>(() => source.ReplaceMethod(firstGenericMethodInfo, firstMethodInfo));
        }

        [Test]
        public void ReplaceNonGenericByGenericMethodThrows()
        {
            Expression<Func<A, string>> source = a => FirstMethod(a.S);
            Assert.Throws<ArgumentException>(() => source.ReplaceMethod(firstMethodInfo, firstGenericMethodInfo));
        }

        [Test]
        public void ReplaceGenericConstructedMethodByNonGenericMethod()
        {
            Expression<Func<A, string>> source = a => FirstMethod<string>(a.S);
            Expression<Func<A, string>> replaced = a => FirstMethod(a.S);
            DoTest(source.ReplaceMethod(firstConstructedGenericMethodInfo, firstMethodInfo), replaced);
        }

        [Test]
        public void ReplaceNonGenericMethodByConstructedGenericMethod()
        {
            Expression<Func<A, string>> source = a => FirstMethod(a.S);
            Expression<Func<A, string>> replaced = a => FirstMethod<string>(a.S);
            DoTest(source.ReplaceMethod(firstMethodInfo, firstConstructedGenericMethodInfo), replaced);
        }

        [Test]
        public void ReplaceGenericStaticClassMethod()
        {
            Expression<Func<A, string>> source = a => Generic<string>.FirstMethod(a.S);
            Expression<Func<A, string>> replaced = a => Generic<string>.SecondMethod(a.S);

            DoTest(source.ReplaceMethod(genericClassFirstMethodInfo, genericClassSecondMethodInfo), replaced);
        }

        [Test]
        public void ThrowsOnMethodsWithDifferentParametersCount()
        {
            Expression<Func<A, string>> source = a => MethodWithTwoParameters(a.S, a.B.S);
            Assert.Throws<ArgumentException>(() => source.ReplaceMethod(methodWithTwoParametersMethodInfo, firstMethodInfo));

            source = a => FirstMethod(a.S);
            Assert.Throws<ArgumentException>(() => source.ReplaceMethod(firstMethodInfo, methodWithTwoParametersMethodInfo));
        }

        [Test]
        public void ThrowsOnMethodsWithDifferentGenericParametersCount()
        {
            Expression<Func<A, string>> source = a => FirstMethod<string>(a.S);
            Assert.Throws<ArgumentException>(() => source.ReplaceMethod(firstGenericMethodInfo, methodWithTwoGenericParametersMethodInfo));

            source = a => MethodWithTwoGenericParameters(a.S, a.N);
            Assert.Throws<ArgumentException>(() => source.ReplaceMethod(methodWithTwoGenericParametersMethodInfo, firstGenericMethodInfo));
        }

        private void DoTest([NotNull] Expression actual, [NotNull] Expression expected)
        {
            Assert.That(ExpressionEquivalenceChecker.Equivalent(actual, expected, strictly : false, distinguishEachAndCurrent : true),
                        () => $"Expected:\n{expected}\nBut was:\n{actual}");
        }

        private static string FirstMethod(string s) => s;
        private static string SecondMethod(string s) => s + s;

        private static T FirstMethod<T>(T t) => t;
        private static T SecondMethod<T>(T t) => default(T);

        private static string MethodWithTwoParameters(string s, string t) => s;
        private static string MethodWithTwoGenericParameters<T1, T2>(T1 a, T2 b) => a.ToString() + b.ToString();

        private static readonly MethodInfo firstMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => FirstMethod(s))).Body).Method;
        private static readonly MethodInfo secondMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => SecondMethod(s))).Body).Method;

        private static readonly MethodInfo methodWithTwoParametersMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => MethodWithTwoParameters(s, s))).Body).Method;
        private static readonly MethodInfo methodWithTwoGenericParametersMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => MethodWithTwoGenericParameters(s, s))).Body).Method.GetGenericMethodDefinition();

        private static readonly MethodInfo firstGenericMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => FirstMethod<string>(s))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo firstConstructedGenericMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => FirstMethod<string>(s))).Body).Method;
        private static readonly MethodInfo secondGenericMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => SecondMethod<string>(s))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo secondConstructedGenericMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => SecondMethod<string>(s))).Body).Method;

        private static readonly MethodInfo genericClassFirstMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => Generic<string>.FirstMethod(s))).Body).Method;
        private static readonly MethodInfo genericClassSecondMethodInfo = ((MethodCallExpression)((Expression<Func<string, string>>)(s => Generic<string>.SecondMethod(s))).Body).Method;

        private class A
        {
            public B B { get; set; }

            public B[] Bs { get; set; }

            public string S { get; set; }

            public int N { get; set; }

            public string FirstMethod(string s) => s;

            public string SecondMethod(string s) => null;

            public static readonly MethodInfo FirstMethodInfo = ((MethodCallExpression)((Expression<Func<A, string>>)(a => a.FirstMethod(a.S))).Body).Method;
            public static readonly MethodInfo SecondMethodInfo = ((MethodCallExpression)((Expression<Func<A, string>>)(a => a.SecondMethod(a.S))).Body).Method;
        }

        private class B
        {
            public string S { get; set; }
        }

        private static class Generic<T>
        {
            public static T FirstMethod(T t)
            {
                return t;
            }

            public static T SecondMethod(T t)
            {
                return default(T);
            }
        }
    }
}