using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class InterfaceMemberResolver : ExpressionVisitor
    {
        /// <summary>
        /// Удаляет касты к интерфейсам, чтобы упростить жизнь компилятору
        /// </summary>
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
            // todo возможно, например, members.Length != 1 в случае явной реализации интерфейса. В этом случае нужно искать какой-то хороший MemberInfo
            return members.Length != 1 ? node.Update(expression) : Expression.MakeMemberAccess(expression, members[0]);
        }
    }
}