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
        public LambdaExpression Lambda { get; private set; }

        public ExpressionCanonicalForm(Expression source)
        {
            Source = source;
            ParameterAccessor = Expression.Parameter(typeof(object[]));

            Expression[] parameters;
            CanonicalForm = new ExpressionCanonizer(ParameterAccessor).Canonize(Source, out parameters);
            ExtractedExpressions = parameters;

            Lambda = Expression.Lambda(CanonicalForm, ParameterAccessor);
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
}
