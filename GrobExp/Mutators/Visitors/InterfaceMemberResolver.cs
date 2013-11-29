using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class InterfaceMemberResolver : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if(node.Member.DeclaringType == null || !node.Member.DeclaringType.IsInterface)
                return base.VisitMember(node);
            Expression expression;
            if(node.Expression != null && node.Expression.NodeType == ExpressionType.Convert && node.Expression.Type.IsInterface)
                expression = Visit(((UnaryExpression)node.Expression).Operand);
            else
                expression = Visit(node.Expression);
            var members = expression.Type.GetMember(node.Member.Name);
            return members.Length != 1 ? node.Update(expression) : Expression.MakeMemberAccess(expression, members[0]);
        }
    }
}