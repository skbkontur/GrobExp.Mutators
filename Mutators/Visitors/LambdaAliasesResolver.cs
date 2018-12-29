using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    internal class LambdaAliasesResolver
    {
        public LambdaAliasesResolver(List<KeyValuePair<Expression, Expression>> aliases)
        {
            this.aliases = aliases;
        }

        public LambdaExpression Resolve(LambdaExpression expression)
        {
            if (expression == null)
                return null;
            var aliasesResolver = new AliasesResolver(aliases);
            var result = (LambdaExpression)aliasesResolver.Visit((Expression)expression);
            var newParameters = new List<ParameterExpression>();
            foreach (var parameter in expression.Parameters)
                newParameters.Add(aliasesResolver.TargetParameterToReplacementParameterMapping.TryGetValue(parameter, out var newParameter) ? newParameter : parameter);
            return Expression.Lambda(result.Body, newParameters);
        }

        private readonly List<KeyValuePair<Expression, Expression>> aliases;
    }
}