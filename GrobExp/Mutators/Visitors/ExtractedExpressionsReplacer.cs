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
            FieldInfo replacement;
            if(node != null && replacements.TryGetValue(node, out replacement))
            {
                if(replacement.FieldType == node.Type)
                    return Expression.Field(parameterAccessor, replacement);
                return Expression.Convert(Expression.Field(parameterAccessor, replacement), node.Type);
            }
            return base.Visit(node);
        }

        private Dictionary<Expression, FieldInfo> replacements;
        private ParameterExpression parameterAccessor;

    }
}
