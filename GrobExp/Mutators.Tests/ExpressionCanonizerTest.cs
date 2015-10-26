using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    class ExpressionCanonizerTest : TestBase
    {
        private Expression<Func<object[], bool>> CompileLambda(LambdaExpression lambda, params ParameterExpression[] namesToExtract)
        {
            var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
            Expression[] parameters;
            return Expression.Lambda<Func<object[], bool>>(
                new ExpressionCanonizer(expressionsParameter, namesToExtract).Canonize(lambda.Body, out parameters),
                expressionsParameter
                );
        }

        [Test]
        public void TestExtractingParameter()
        {
            var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
            Expression[] parameters;
            var lambdaToTest = (Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.S.Length > 1);
            var compiledLambda = Expression.Lambda<Func<object[], bool>>(
                new ExpressionCanonizer(expressionsParameter, lambdaToTest.Parameters[0]).Canonize(lambdaToTest.Body, out parameters),
                expressionsParameter
                ).Compile();
            Assert.IsTrue(compiledLambda.Invoke(new object[] { 2, 1 }));
            Assert.AreEqual(2, parameters.Length);
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(
                parameters[0],
                Expression.Property(Expression.Property(Expression.Parameter(typeof(ValidatorsTest.TestData)), "S"), "Length"), false, true));
        }

        [Test]
        public void TestEquivalentAfterExtracting()
        {
            var lambda1 = (Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.S.Length > 1);
            var lambda2 = (Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.D.S.Length > 1);
            Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
            var extracted1 = CompileLambda(lambda1, lambda1.Parameters[0]);
            var extracted2 = CompileLambda(lambda2, lambda2.Parameters[0]);
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(extracted1, extracted2, true, false));
        }

        [Test]
        public void TestComplexExpressions()
        {
            var lambda1 = (Expression<Func<ValidatorsTest.TestData, bool>>)(q => q.S.Length > 1 && q.A.S.Length < 0);
            var lambda2 = (Expression<Func<ValidatorsTest.TestData, bool>>)(q => q.D.S.Length > 1 && q.A.B[0].S.Length < 0);
            Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
            var extracted1 = CompileLambda(lambda1, lambda1.Parameters[0]);
            var extracted2 = CompileLambda(lambda2, lambda2.Parameters[0]);
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(extracted1, extracted2, true, false));
        }

        [Test]
        public void TestMultipleParameters()
        {
            var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
            Expression[] parameters;
            var lambda = (Expression<Func<ValidatorsTest.TestData, int, bool>>)((z, k) => z.A.S.Length + k > 0);
            var newLambda = Expression.Lambda<Func<object[], int, bool>>(new ExpressionCanonizer(expressionsParameter, lambda.Parameters[0])
                .Canonize(lambda.Body, out parameters),
                new []{expressionsParameter}.Concat(lambda.Body.ExtractParameters().Skip(1))
                ).Compile();
            Assert.IsTrue(newLambda.Invoke(new object[]{2, 4}, 3));
            Assert.IsFalse(newLambda.Invoke(new object[]{2, 5}, 3));
        }

        [Test]
        public void TestExtractManyParameters()
        {
            var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
            Expression[] parameters;
            var lambda = (Expression<Func<ValidatorsTest.TestData, int, int, bool>>)((z, k, p) => z.A.S.Length + k - p > 0);
            var parametersToExtract = new []{lambda.Parameters[0], lambda.Parameters[2]};
            var newLambda = Expression.Lambda<Func<object[], int, bool>>(new ExpressionCanonizer(expressionsParameter, parametersToExtract)
                                                                             .Canonize(lambda.Body, out parameters),
                                                                         new[] {expressionsParameter}.Concat(lambda.Parameters.Where(p => !parametersToExtract.Contains(p)))).Compile();
            Assert.IsTrue(newLambda.Invoke(new object[]{2, 2, 0}, 1));
            Assert.IsFalse(newLambda.Invoke(new object[]{3, 1, 10}, 2));
        }

        [Test]
        public void TestConstructCanonicalFormInvokation()
        {
            var lambda = (Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.A.S.Length > 2);
            var canonicalForm = new ExpressionCanonicalForm(lambda.Body, lambda.Parameters[0]);
            var newLambda = Expression.Lambda<Func<ValidatorsTest.TestData, bool>>(canonicalForm.ConstructInvokation(Expression.Lambda(canonicalForm.CanonicalForm, canonicalForm.ParameterAccessor)), lambda.Parameters[0]).Compile();

            Assert.IsTrue(newLambda.Invoke(new ValidatorsTest.TestData{A = new ValidatorsTest.A {S = "zzz"}}));
            Assert.IsFalse(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zz" } }));
        }

        [Test]
        public void TestConstructInvokationWhenExtractedManyParameters()
        {
            var lambda = (Expression<Func<ValidatorsTest.TestData, int, int, bool>>)((z, m, k) => z.A.S.Length - m > k);
            var canonicalForm = new ExpressionCanonicalForm(lambda.Body, lambda.Parameters[0], lambda.Parameters[2]);
            var newLambdaBody = Expression.Lambda<Func<ValidatorsTest.TestData, int, int, bool>>(canonicalForm.ConstructInvokation(Expression.Lambda(canonicalForm.CanonicalForm, canonicalForm.ParameterAccessor)), lambda.Parameters);
            var newLambda = newLambdaBody.Compile();

            Assert.IsTrue(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zzzz" } }, 1, 2));
            Assert.IsFalse(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zzzz" } }, 2, 2));
        }

        [Test]
        public void TestGroupBy()
        {
            var lambda = (Expression<Func<ValidatorsTest.TestData, int, int, bool>>)((z, m, k) => z.A.S.Length - m > k);
            var canonicalForm1 = new ExpressionCanonicalForm(lambda.Body, lambda.Parameters[0], lambda.Parameters[2]);
            var canonicalForm2 = new ExpressionCanonicalForm(lambda.Body, lambda.Parameters[0], lambda.Parameters[2]);
            var grouping = new[] {canonicalForm1, canonicalForm2}.GroupBy(form => form, form => form, new CanonicalFormEqualityComparer());
            Assert.AreEqual(1, grouping.Count());
        }
    }
}
