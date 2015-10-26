using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class ExpressionCanonicalForm
    {
        public Expression Source { get; private set; }
        public Expression CanonicalForm { get; private set; }
        public ParameterExpression ParameterAccessor { get; set; }
        private Expression[] ExtractedExpressions { get; set; }

        public ExpressionCanonicalForm(Expression source, params ParameterExpression[] parametersToExtract)
        {
            Source = source;
            ParameterAccessor = Expression.Parameter(typeof(object[]));
            Expression[] parameters;
            CanonicalForm = new ExpressionCanonizer(ParameterAccessor, parametersToExtract).Canonize(Source, out parameters);
            ExtractedExpressions = parameters;
        }

        public Expression ConstructInvokation(LambdaExpression lambda)
        {
            var array = Expression.Parameter(typeof(object[]));
            var newArrayInit = Expression.NewArrayInit(typeof(object), ExtractedExpressions.Select(exp => Expression.Convert(exp, typeof(object))));
            return Expression.Block(new []{ array },
                Expression.Assign(array, newArrayInit), 
                Expression.Invoke(lambda, array)
            );
        }
    }

    public class CanonicalFormEqualityComparer : IEqualityComparer<ExpressionCanonicalForm>
    {
        public bool Equals(ExpressionCanonicalForm x, ExpressionCanonicalForm y)
        {
            return ExpressionEquivalenceChecker.Equivalent(x.CanonicalForm, y.CanonicalForm, false, true);
        }

        public int GetHashCode(ExpressionCanonicalForm obj)
        {
            return 0;
        }
    }
}
