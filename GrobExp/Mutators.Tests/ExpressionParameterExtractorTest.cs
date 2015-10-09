using System;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
	class ExpressionParameterExtractorTest : TestBase
	{
		private Expression<Func<object[], bool>> CompileLambda(Expression lambda)
		{
			var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
			object[] parameters;
			return Expression.Lambda<Func<object[], bool>>(
				new ExpressionParametersExtractor(expressionsParameter).ExtractParameters(lambda, out parameters),
				expressionsParameter
				);
		}

		[Test]
		public void TestExtractingParameter()
		{
			var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
			object[] parameters;
			var lambdaToTest = ((Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.S.Length > 1)).Body;
			var compiledLambda = Expression.Lambda<Func<object[], bool>>(
				new ExpressionParametersExtractor(expressionsParameter).ExtractParameters(lambdaToTest, out parameters),
				expressionsParameter
				).Compile();
			Assert.IsTrue(compiledLambda.Invoke(new object[]{2}));
		}

		[Test]
		public void TestEquivalentAfterExtracting()
		{
			var lambda1 = ((Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.S.Length > 1));
			var lambda2 = ((Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.D.S.Length > 1));
			Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
			var extracted1 = CompileLambda(lambda1.Body);
			var extracted2 = CompileLambda(lambda2.Body);
			Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(extracted1, extracted2, true, false));
		}

		[Test]
		public void TestComplexExpressions()
		{
			var lambda1 = ((Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.S.Length > 1 && z.A.S.Length < 0));
			var lambda2 = ((Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.D.S.Length > 1 && z.A.B[0].S.Length < 0));
			Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
			var extracted1 = CompileLambda(lambda1.Body);
			var extracted2 = CompileLambda(lambda2.Body);
			Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(extracted1, extracted2, true, false));
		}
	}
}
