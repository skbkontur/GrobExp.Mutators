using System;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using JetBrains.Annotations;

using NUnit.Framework;

namespace Mutators.Tests.Visitors
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class IfNotNullProcessorTest
    {
        [Test]
        public void NoIfNotNulls()
        {
            Check<object, object, bool>(
                (x, y) => x == y,
                (x, y) => x == y);
        }

        [Test]
        public void IfNotNullObject()
        {
            Check<object, object, bool>(
                (x, y) => x.IfNotNull() == y,
                (x, y) => x == null || x == y);

            Check<object, object, bool>(
                (x, y) => x == y.IfNotNull(),
                (x, y) => y == null || x == y);

            Check<object, object, bool>(
                (x, y) => x.IfNotNull() == y.IfNotNull(),
                (x, y) => x == null || (y == null || x == y));
        }

        [Test]
        public void IfNotNullNonConstantString()
        {
            Check<string, string, bool>(
                (x, y) => x.IfNotNull() == y,
                (x, y) => x == null || x == y);

            Check<string, string, bool>(
                (x, y) => x == Guid.NewGuid().ToString().IfNotNull(),
                (x, y) => Guid.NewGuid().ToString() == null || x == Guid.NewGuid().ToString());

            Check<string, string, bool>(
                (x, y) => x.Dynamic().IfNotNull() == y.IfNotNull(),
                (x, y) => x.Dynamic() == null || (y == null || x.Dynamic() == y));
        }

        [Test]
        public void IfNotNullConstantString()
        {
            Check<string, string, bool>(
                (x, y) => "".IfNotNull() == y,
                (x, y) => string.IsNullOrEmpty("") || "" == y);

            Func<string> constString = () => "GRobas";
            Check<string, string, bool>(
                (x, y) => x == constString().IfNotNull(),
                (x, y) => string.IsNullOrEmpty("GRobas") || x == "GRobas");

            Expression<Func<string, string, bool>> rawExpression = (x, y) => typeof(string).Name.IfNotNull() == constString().IfNotNull();
            // Compiler substitutes constant comparison expression "String" == "GRobas" with the result(false), so we have to construct the expectation manually
            Expression<Func<string>> expectedPrefix = () => "GRobas";
            Expression<Func<string, bool>> expectedSuffix = x => string.IsNullOrEmpty("String") || (string.IsNullOrEmpty("GRobas") || "String" == x);
            var expectedExpression = Expression.Lambda<Func<string, string, bool>>(expectedPrefix.Merge(expectedSuffix).Body, rawExpression.Parameters);
            Check(rawExpression, expectedExpression);
        }

        private void Check<TArg1, TArg2, TResult>(
            [NotNull] Expression<Func<TArg1, TArg2, TResult>> rawExpression,
            [NotNull] Expression<Func<TArg1, TArg2, TResult>> expectedExpression)
        {
            var actualExpression = new IfNotNullProcessor().Visit(rawExpression);
            Assert.True(ExpressionEquivalenceChecker.Equivalent(actualExpression, expectedExpression, strictly : false, distinguishEachAndCurrent : true),
                        "Failed to eliminate IfNotNull:\nExpected to get '{0}',\n        but got '{1}'", expectedExpression, actualExpression);
        }
    }
}