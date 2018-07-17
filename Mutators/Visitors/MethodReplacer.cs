using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public class MethodReplacer : ExpressionVisitor
    {
        public MethodReplacer(MethodInfo from, MethodInfo to)
        {
            this.from = from;
            this.to = to;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if (method == from)
                return Expression.Call(Visit(node.Object), to, node.Arguments.Select(Visit));
            if (method.IsGenericMethod && method.GetGenericMethodDefinition() == from)
                return Expression.Call(Visit(node.Object), to.MakeGenericMethod(method.GetGenericArguments()), node.Arguments.Select(Visit));
            return base.VisitMethodCall(node);
        }

        private readonly MethodInfo from;
        private readonly MethodInfo to;
    }
}