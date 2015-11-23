using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionCanonizer : ExpressionVisitor
    {
        public Expression Canonize(Expression expression, Expression[] expressionsToReplace)
        {
            this.expressionsToReplace = expressionsToReplace.ToDictionary(exp => exp, exp => Expression.Parameter(exp.Type));
            return Visit(expression);
        }

        public override Expression Visit(Expression node)
        {
            return node != null && expressionsToReplace.ContainsKey(node) ? expressionsToReplace[node] : base.Visit(node);
        }

        private Dictionary<Expression, ParameterExpression> expressionsToReplace;
    }
}