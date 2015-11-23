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
        [Test]
        public void TestEquivalentCanonicalForms()
        {
            var lambda1 = (Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.S.Length > 1);
            var lambda2 = (Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.D.S.Length > 1);
            Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
            var form1 = new ExpressionCanonicalForm(lambda1.Body).CanonicalForm;
            var form2 = new ExpressionCanonicalForm(lambda2.Body).CanonicalForm;
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(form1, form2, false, false));
        }

        [Test]
        public void TestComplexExpressions()
        {
            var lambda1 = (Expression<Func<ValidatorsTest.TestData, bool>>)(q => q.S.Length > 1 && q.A.S.Length < 0);
            var lambda2 = (Expression<Func<ValidatorsTest.TestData, bool>>)(q => q.D.S.Length > 1 && q.A.B[0].S.Length < 0);
            Assert.IsFalse(ExpressionEquivalenceChecker.Equivalent(lambda1, lambda2, true, false));
            var form1 = new ExpressionCanonicalForm(lambda1.Body).CanonicalForm;
            var form2 = new ExpressionCanonicalForm(lambda2.Body).CanonicalForm;
            Assert.IsTrue(ExpressionEquivalenceChecker.Equivalent(form1, form2, false, false));
        }

        [Test]
        public void TestConstructCanonicalFormInvokation()
        {
            var lambda = (Expression<Func<ValidatorsTest.TestData, bool>>)(z => z.A.S.Length > 2);
            var canonicalForm = new ExpressionCanonicalForm(lambda.Body);
            var l = LambdaCompiler.Compile(canonicalForm.GetLambda(), CompilerOptions.All);
            var newLambda = Expression.Lambda<Func<ValidatorsTest.TestData, bool>>(canonicalForm.ConstructInvokation(l), lambda.Parameters).Compile();

            Assert.IsTrue(newLambda.Invoke(new ValidatorsTest.TestData{A = new ValidatorsTest.A {S = "zzz"}}));
            Assert.IsFalse(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zz" } }));
        }

       [Test]
       public void TestConstructInvokationWhenExtractedManyParameters()
       {
           var lambda = (Expression<Func<ValidatorsTest.TestData, int, int, bool>>)((z, m, k) => z.A.S.Length - m > k);
           var canonicalForm = new ExpressionCanonicalForm(lambda.Body);
           var l = LambdaCompiler.Compile(canonicalForm.GetLambda(), CompilerOptions.All);
           var newLambdaBody = Expression.Lambda<Func<ValidatorsTest.TestData, int, int, bool>>(canonicalForm.ConstructInvokation(l), lambda.Parameters);
           var newLambda = LambdaCompiler.Compile(newLambdaBody, CompilerOptions.All);

           Assert.IsTrue(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zzzz" } }, 1, 2));
           Assert.IsFalse(newLambda.Invoke(new ValidatorsTest.TestData { A = new ValidatorsTest.A { S = "zzzz" } }, 2, 2));
        }
    }
}
