using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    class ExpressionParameterExtractorTest : TestBase
    {
        private Expression<Func<object[], bool>> CompileLambda(LambdaExpression lambda, params ParameterExpression[] namesToExtract)
        {
            var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
            object[] parameters;
            return Expression.Lambda<Func<object[], bool>>(
                new ExpressionParametersExtractor(expressionsParameter, namesToExtract).ExtractParameters(lambda.Body, out parameters),
                expressionsParameter
                );
        }

        [Test]
        public void TestExtractingParameter()
        {
            var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
            object[] parameters;
            var lambdaToTest = ((Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.S.Length > 1));
            var compiledLambda = Expression.Lambda<Func<object[], bool>>(
                new ExpressionParametersExtractor(expressionsParameter, lambdaToTest.Parameters[0]).ExtractParameters(lambdaToTest.Body, out parameters),
                expressionsParameter
                ).Compile();
            Assert.IsTrue(compiledLambda.Invoke(new object[] { 2 }));
        }

        [Test]
        public void TestEquivalentAfterExtracting()
        {
            var lambda1 = ((Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.S.Length > 1));
            var lambda2 = ((Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.D.S.Length > 1));
            Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
            var extracted1 = CompileLambda(lambda1, lambda1.Parameters[0]);
            var extracted2 = CompileLambda(lambda2, lambda2.Parameters[0]);
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(extracted1, extracted2, true, false));
        }

        [Test]
        public void TestComplexExpressions()
        {
            var lambda1 = ((Expression<Func<ValidatorsTest.TestData, bool>>)(q => q.S.Length > 1 && q.A.S.Length < 0));
            var lambda2 = ((Expression<Func<ValidatorsTest.TestData, bool>>)(q => q.D.S.Length > 1 && q.A.B[0].S.Length < 0));
            Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
            var extracted1 = CompileLambda(lambda1);
            var extracted2 = CompileLambda(lambda2);
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(extracted1, extracted2, true, false));
        }
    }
}
