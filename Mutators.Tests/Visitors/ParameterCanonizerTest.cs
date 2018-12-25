using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests.Visitors
{
    [TestFixture]
    public class ParameterCanonizerTest
    {
        [Test]
        public void ParameterExpression()
        {
            var parameter = Expression.Parameter(typeof(int));

            DoTest(source : parameter, expected : parameter);
        }

        [Test]
        public void TwoUsagesOfParameter()
        {
            var parameter = Expression.Parameter(typeof(int));
            var sumExpression = Expression.Add(parameter, parameter);

            DoTest(source : sumExpression, expected : sumExpression);
        }

        [Test]
        public void TwoParametersOfDifferentTypes()
        {
            var intParameter = Expression.Parameter(typeof(int));
            var doubleParameter = Expression.Parameter(typeof(double));
            var sumExpression = Expression.Add(Expression.Convert(intParameter, typeof(double)), doubleParameter);

            DoTest(source : sumExpression, expected : sumExpression);
        }

        [Test]
        public void TwoParametersOfSameType()
        {
            var firstParameter = Expression.Parameter(typeof(int), "a");
            var secondParameter = Expression.Parameter(typeof(int), "b");

            DoTest(source : Expression.Add(firstParameter, secondParameter),
                   expected : Expression.Add(firstParameter, firstParameter));
        }

        [Test]
        public void TwoVariablesOfSameType()
        {
            var firstParameter = Expression.Variable(typeof(int), "a");
            var secondParameter = Expression.Variable(typeof(int), "b");

            DoTest(source : Expression.Add(firstParameter, secondParameter),
                   expected : Expression.Add(firstParameter, firstParameter));
        }

        [Test]
        public void IgnoreLocalVariables()
        {
            var firstParameter = Expression.Parameter(typeof(int));
            var secondParameter = Expression.Parameter(typeof(int));

            var variable = Expression.Variable(typeof(int));
            var assign = Expression.Assign(variable, Expression.Constant(5));

            var sourceSum = Expression.Add(Expression.Add(variable, firstParameter), secondParameter);
            var expectedSum = Expression.Add(Expression.Add(variable, firstParameter), firstParameter);

            DoTest(source : Expression.Block(typeof(int), variables : new[] {variable}, expressions : new Expression[] {assign, sourceSum}),
                   expected : Expression.Block(typeof(int), variables : new[] {variable}, expressions : new Expression[] {assign, expectedSum}));
        }

        [Test]
        public void IgnoreNestedLambdaParameters()
        {
            var firstParameter = Expression.Parameter(typeof(int));
            var secondParameter = Expression.Parameter(typeof(int));

            var lambdaParameter = Expression.Variable(typeof(int));
            var assign = Expression.Assign(lambdaParameter, Expression.Constant(5));

            var sourceSum = Expression.Add(Expression.Add(lambdaParameter, firstParameter), secondParameter);
            var expectedSum = Expression.Add(Expression.Add(lambdaParameter, firstParameter), firstParameter);

            DoTest(source : Expression.Lambda(Expression.Block(typeof(int), expressions : new Expression[] {assign, sourceSum}), lambdaParameter),
                   expected : Expression.Lambda(Expression.Block(typeof(int), expressions : new Expression[] {assign, expectedSum}), lambdaParameter));
        }

        private void DoTest(Expression source, Expression expected)
        {
            Assert.That(ExpressionEquivalenceChecker.Equivalent(source.CanonizeParameters(), expected, strictly : true, distinguishEachAndCurrent : true));
        }
    }
}