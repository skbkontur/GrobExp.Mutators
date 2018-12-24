using System.Linq.Expressions;

using JetBrains.Annotations;

namespace GrobExp.Mutators.Visitors
{
    public class InterfaceMemberResolver : ExpressionVisitor
    {
        /// <summary>
        ///     Removes redundant casts to interface in order to simplify expressions compiling
        /// </summary>
        protected override Expression VisitMember([NotNull] MemberExpression node)
        {
            if (node.Member.DeclaringType == null || !node.Member.DeclaringType.IsInterface)
                return base.VisitMember(node);
            if (node.Expression?.NodeType != ExpressionType.Convert)
                return base.VisitMember(node);

            var expressionBeforeConvert = Visit(((UnaryExpression)node.Expression).Operand);

            // Should be impossible to define two interface members having the same name
            // Case when no members match by name should also be impossible
            var members = expressionBeforeConvert.Type.GetMember(node.Member.Name);
            return members.Length != 1 ? node.Update(expressionBeforeConvert) : Expression.MakeMemberAccess(expressionBeforeConvert, members[0]);
        }
    }
}