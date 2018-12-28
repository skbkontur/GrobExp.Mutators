using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace GrobExp.Mutators.Visitors
{
    /// <summary>
    /// Replaces outer scope parameters of same type by first occurence of parameter of this type.
    /// Ignores occurrences of parameters of inner lambdas and variables declared inside.
    /// </summary>
    internal class ParameterCanonizer : ExpressionVisitor
    {
        [NotNull]
        public Expression Canonize([NotNull] Expression expression)
        {
            parameters.Clear();
            localParameters.Clear();
            return Visit(expression);
        }

        [NotNull]
        protected override Expression VisitLambda<T>([NotNull] Expression<T> lambda)
        {
            foreach (var parameter in lambda.Parameters)
                localParameters.Add(parameter);
            var res = base.VisitLambda(lambda);
            foreach (var parameter in lambda.Parameters)
                localParameters.Remove(parameter);
            return res;
        }

        [NotNull]
        protected override Expression VisitBlock([NotNull] BlockExpression node)
        {
            foreach (var variable in node.Variables)
                localParameters.Add(variable);
            var res = base.VisitBlock(node);
            foreach (var variable in node.Variables)
                localParameters.Remove(variable);
            return res;
        }

        [NotNull]
        protected override Expression VisitParameter([NotNull] ParameterExpression node)
        {
            if (localParameters.Contains(node))
                return node;
            if (parameters.TryGetValue(node.Type, out var parameter))
                return parameter;
            parameters.Add(node.Type, node);
            return node;
        }

        private readonly Dictionary<Type, ParameterExpression> parameters = new Dictionary<Type, ParameterExpression>();
        private readonly HashSet<ParameterExpression> localParameters = new HashSet<ParameterExpression>();
    }
}