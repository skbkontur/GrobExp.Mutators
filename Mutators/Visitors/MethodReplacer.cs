using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace GrobExp.Mutators.Visitors
{
    public class MethodReplacer : ExpressionVisitor
    {
        public MethodReplacer([NotNull] params (MethodInfo From, MethodInfo To)[] replacements)
        {
            foreach (var (from, to) in replacements)
                methodReplacements[from] = to;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;

            if (methodReplacements.TryGetValue(method, out var replacement))
                return Expression.Call(Visit(node.Object), replacement, node.Arguments.Select(Visit));

            if (method.IsGenericMethod && methodReplacements.TryGetValue(method.GetGenericMethodDefinition(), out replacement))
                return Expression.Call(Visit(node.Object), replacement.MakeGenericMethod(method.GetGenericArguments()), node.Arguments.Select(Visit));

            return base.VisitMethodCall(node);
        }

        private readonly Dictionary<MethodInfo, MethodInfo> methodReplacements = new Dictionary<MethodInfo, MethodInfo>();
    }
}