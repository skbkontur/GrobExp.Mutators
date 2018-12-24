using System.Linq.Expressions;

using JetBrains.Annotations;

namespace GrobExp.Mutators.Visitors
{
    public class InterfaceMemberResolver : ExpressionVisitor
    {
        /// <summary>
        ///     Удаляет касты к интерфейсам, чтобы упростить жизнь компилятору
        /// </summary>
        protected override Expression VisitMember([NotNull] MemberExpression node)
        {
            if (node.Member.DeclaringType == null || !node.Member.DeclaringType.IsInterface)
                return base.VisitMember(node);
            if (node.Expression?.NodeType != ExpressionType.Convert)
                return base.VisitMember(node);
            var expression = Visit(((UnaryExpression)node.Expression).Operand);
            var members = expression.Type.GetMember(node.Member.Name);
            return members.Length != 1 ? node.Update(expression) : Expression.MakeMemberAccess(expression, members[0]);
        }
    }
}