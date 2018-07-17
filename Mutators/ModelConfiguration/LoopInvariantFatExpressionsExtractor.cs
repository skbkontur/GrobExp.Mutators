using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class LoopInvariantFatExpressionsExtractor
    {
        public static Expression ExtractLoopInvariantFatExpressions(this Expression expression, IEnumerable<ParameterExpression> invariantParameters, Func<Expression, Expression> resultSelector)
        {
            var extractedExpressions = new InvariantCodeExtractor(invariantParameters).Extract(expression);
            if (extractedExpressions.Length == 0)
                return resultSelector(expression);

            var aliases = new Dictionary<Expression, Expression>();
            var variables = new List<ParameterExpression>();
            foreach (var exp in extractedExpressions)
            {
                if (!aliases.ContainsKey(exp))
                {
                    var variable = Expression.Variable(exp.Type);
                    variables.Add(variable);
                    aliases.Add(exp, variable);
                }
            }

            var optimizedExpression = new ExpressionReplacer(aliases).Visit(expression);
            return Expression.Block(variables, aliases.Select(pair => Expression.Assign(pair.Value, pair.Key)).Concat(new[] {resultSelector(optimizedExpression)}));
        }
    }
}