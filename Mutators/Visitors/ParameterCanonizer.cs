using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ParameterCanonizer : ExpressionVisitor
    {
        public Expression Canonize(Expression expression)
        {
            parameters.Clear();
            localParameters.Clear();
            return Visit(expression);
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            foreach (var parameter in lambda.Parameters)
                localParameters.Add(parameter);
            var res = base.VisitLambda(lambda);
            foreach (var parameter in lambda.Parameters)
                localParameters.Remove(parameter);
            return res;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach (var variable in node.Variables)
                localParameters.Add(variable);
            var res = base.VisitBlock(node);
            foreach (var variable in node.Variables)
                localParameters.Remove(variable);
            return res;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (localParameters.Contains(node))
                return node;
            ParameterExpression parameter;
            if (parameters.TryGetValue(node.Type, out parameter))
                return parameter;
            parameters.Add(node.Type, node);
            return node;
        }

        private readonly Dictionary<Type, ParameterExpression> parameters = new Dictionary<Type, ParameterExpression>();
        private readonly HashSet<ParameterExpression> localParameters = new HashSet<ParameterExpression>();
    }
}