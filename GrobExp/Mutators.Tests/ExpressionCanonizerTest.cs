using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    class ExpressionCanonizerTest : TestBase
    {
        private Expression<Func<object[], bool>> CompileLambda(LambdaExpression lambda)
        {
            var expressionsParameter = Expression.Parameter(typeof(object[]), "exprs");
            Expression[] parameters;
            return Expression.Lambda<Func<object[], bool>>(
                new ExpressionCanonizer(expressionsParameter).Canonize(lambda.Body, out parameters),
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
                new ExpressionCanonizer(expressionsParameter).Canonize(lambdaToTest.Body, out parameters),
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
            var extracted1 = CompileLambda(lambda1);
            var extracted2 = CompileLambda(lambda2);
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(extracted1, extracted2, true, false));
        }

        [Test]
        public void TestComplexExpressions()
        {
            var lambda1 = (Expression<Func<ValidatorsTest.TestData, bool>>)(q => q.S.Length > 1 && q.A.S.Length < 0);
            var lambda2 = (Expression<Func<ValidatorsTest.TestData, bool>>)(q => q.D.S.Length > 1 && q.A.B[0].S.Length < 0);
            Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
            var extracted1 = CompileLambda(lambda1);
            var extracted2 = CompileLambda(lambda2);
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(extracted1, extracted2, true, false));
        }

       /* [Test]
        public void TestConstructCanonicalFormInvokation()
        {
            var lambda = (Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.A.S.Length > 2);
            var canonicalForm = new ExpressionCanonicalForm(lambda.Body);
            var l = (Func<object[], bool>)LambdaCompiler.Compile(canonicalForm.Lambda, CompilerOptions.All);
            var newLambda = Expression.Lambda<Func<ValidatorsTest.TestData, bool>>(canonicalForm.ConstructInvokation(l), lambda.Parameters).Compile();

            Assert.IsTrue(newLambda.Invoke(new ValidatorsTest.TestData{A = new ValidatorsTest.A {S = "zzz"}}));
            Assert.IsFalse(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zz" } }));
        }

       /* [Test]
        public void TestConstructInvokationWhenExtractedManyParameters()
        {
            var lambda = (Expression<Func<ValidatorsTest.TestData, int, int, bool>>)((z, m, k) => z.A.S.Length - m > k);
            var canonicalForm = new ExpressionCanonicalForm(lambda.Body);
            var newLambdaBody = Expression.Lambda<Func<ValidatorsTest.TestData, int, int, bool>>(canonicalForm.ConstructInvokation(Expression.Lambda(canonicalForm.CanonicalForm, canonicalForm.ParameterAccessor)), lambda.Parameters);
            var newLambda = newLambdaBody.Compile();

            Assert.IsTrue(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zzzz" } }, 1, 2));
            Assert.IsFalse(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zzzz" } }, 2, 2));
        }*/
    }
}
