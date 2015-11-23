using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public class ExtractedExpressionsReplacer : ExpressionVisitor
    {
        public Expression Replace(Expression expression, Expression[] extractedExpressions, ParameterExpression parameterAccessor, FieldInfo[] fieldInfos)
        {
            replacements = Enumerable.Range(0, extractedExpressions.Length).ToDictionary(i => extractedExpressions[i], i => fieldInfos[i]);
            this.parameterAccessor = parameterAccessor;
            return Visit(expression);
        }

        public override Expression Visit(Expression node)
        {
            return node != null && replacements.ContainsKey(node) ? Expression.Field(parameterAccessor, replacements[node]) : base.Visit(node);
        }

        private Dictionary<Expression, FieldInfo> replacements;
        private ParameterExpression parameterAccessor;

    }
}
