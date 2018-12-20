using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors.CompositionPerforming;

using JetBrains.Annotations;

using NUnit.Framework;

namespace Mutators.Tests.Visitors.CompositionPerformingTests
{
    [TestFixture]
    public class IsSimpleLinkOfChainCheckerTest
    {
        [Test]
        public void Parameter()
        {
            Expression<Func<int, int>> exp = x => x;

            AssertTrue(exp.Body, typeof(int));
        }

        [Test]
        public void MemberExpression()
        {
            Expression<Func<B, string>> exp = b => b.S;

            AssertTrue(exp.Body, typeof(B));
        }

        [Test]
        public void StringLength_NotChain()
        {
            Expression<Func<string, int>> exp = s => s.Length;
            AssertFalse(exp.Body);
        }

        [Test]
        public void NotChainInMember()
        {
            Expression<Func<A, int>> exp = a => a.B.S.Length;
            AssertFalse(exp.Body);
        }

        [Test]
        public void ConstantArrayIndex()
        {
            Expression<Func<A, B>> exp = a => a.Bs[0];
            AssertTrue(exp.Body, typeof(A));
        }

        [Test]
        public void ArrayIndex()
        {
            Expression<Func<A, B>> exp = a => a.Bs[a.B.S.Length];
            AssertTrue(exp.Body, typeof(A));
        }

        [Test]
        public void EachCall()
        {
            Expression<Func<A, B>> exp = a => a.Bs.Each();
            AssertTrue(exp.Body, typeof(A));
        }

        [Test]
        public void CurrentCall()
        {
            Expression<Func<A, B>> exp = a => a.Bs.Current();
            AssertTrue(exp.Body, typeof(A));
        }

        [Test]
        public void TemplateIndexCall()
        {
            Expression<Func<A, B>> exp = a => a.Bs.TemplateIndex();
            AssertTrue(exp.Body, typeof(A));
        }

        [Test]
        public void WhereCall()
        {
            Expression<Func<A, string>> exp = a => a.Bs.Where(b => b.S == "zzz").Each().S;
            AssertTrue(exp.Body, typeof(A));
        }

        [Test]
        public void DictionaryGetItemCall()
        {
            Expression<Func<A, string>> exp = a => a.CustomBs["zzz"].S;
            AssertTrue(exp.Body, typeof(A));
        }

        [Test]
        public void NotChains()
        {
            AssertFalse(((Expression<Func<int, int, bool>>)((a, b) => a == b)).Body);

            AssertFalse(((Expression<Func<int, object>>)(a => (object)a)).Body);

            AssertFalse(((Expression<Func<int, int>>)(a => MyMethod(a))).Body);

            AssertFalse(((Expression<Func<B, string>>)(b => b.GetS())).Body);

            AssertFalse((Expression<Func<A, string>>)(a => a.B.S));
        }

        private static T MyMethod<T>(T x)
        {
            return x;
        }

        private void AssertFalse(Expression expression)
        {
            Assert.That(IsSimpleLinkOfChainChecker.IsSimpleLinkOfChain(expression, out var type), Is.False);
            Assert.That(type, Is.Null);
        }

        private void AssertTrue(Expression expression, [CanBeNull] Type expectedType)
        {
            Assert.That(IsSimpleLinkOfChainChecker.IsSimpleLinkOfChain(expression, out var type));
            Assert.That(type, Is.EqualTo(expectedType));
        }

        private class A
        {
            public B B { get; set; }

            public B[] Bs { get; set; }

            public Dictionary<string, B> CustomBs { get; set; }
        }

        private class B
        {
            public string S { get; set; }

            public string GetS()
            {
                return S;
            }
        }
    }
}