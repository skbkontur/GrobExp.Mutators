using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.Compiler
{
    class ExpressionFieldsExtractor : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            var member = node.Member;

            var expression = Visit(node.Expression);

            if (expression != null && member.MemberType == MemberTypes.Field && (expression.Type.IsNestedPrivate || !((FieldInfo)member).Attributes.HasFlag(FieldAttributes.Public)))
            {
                var extractor = FieldsExtractor.GetExtractor(member as FieldInfo);
                if(expression.NodeType == ExpressionType.Convert)
                    expression = ((UnaryExpression)expression).Operand;
                return Expression.Convert(Expression.Invoke(Expression.Constant(extractor), expression), node.Type);
            }

            return node.Update(expression);
        }
    }
}