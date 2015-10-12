using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class ExpressionCanonicalForm
    {
        public Expression Source { get; private set; }
        public Expression CanonicalForm { get; private set; }
        private ParameterExpression ParameterAccessor { get; set; }
        private Expression[] ExtractedExpressions { get; set; }

        public ExpressionCanonicalForm(Expression source, params ParameterExpression[] parametersToExtract)
        {
            Source = source;
            ParameterAccessor = Expression.Parameter(typeof(object[]));
            Expression[] parameters;
            CanonicalForm = new ExpressionDependenciesExtractor(ParameterAccessor, parametersToExtract).ExtractParameters(Source, out parameters);
            ExtractedExpressions = parameters;
        }

        public Expression ConstructInvokation()
        {
            var lambda = Expression.Lambda<Func<object[], Expression>>(CanonicalForm, ParameterAccessor);
            return Expression.Invoke(lambda, Expression.NewArrayInit(typeof(ParameterExpression), ExtractedExpressions));
        }
    }
}
