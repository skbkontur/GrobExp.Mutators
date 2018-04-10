using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ParameterExistanceChecker : ExpressionVisitor
    {
        public ParameterExistanceChecker(HashSet<ParameterExpression> parameters)
        {
            this.parameters = parameters;
        }

        public bool HasParameter(Expression node)
        {
            Visit(node);
            return exists;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (parameters.Contains(node))
                exists = true;
            return base.VisitParameter(node);
        }

        private bool exists;
        private readonly HashSet<ParameterExpression> parameters;
    }
}