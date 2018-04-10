using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    /// <summary>
    ///     Проверяет, является ли Expression цепочкой вызовов (а в случае recursive:false - первым звеном такой цепочки)
    /// </summary>
    public static class IsLinkOfChainChecker
    {
        public static bool IsLinkOfChain(this Expression node, bool restrictConstants, bool recursive)
        {
            return node != null &&
                   (node.NodeType == ExpressionType.Parameter
                    || (node.NodeType == ExpressionType.Constant && !restrictConstants)
                    || IsLinkOfChain(node as MemberExpression, restrictConstants, recursive)
                    || IsLinkOfChain(node as BinaryExpression, restrictConstants, recursive)
                    || IsLinkOfChain(node as UnaryExpression, restrictConstants, recursive)
                    || IsLinkOfChain(node as MethodCallExpression, restrictConstants, recursive));
        }

        private static bool IsLinkOfChain(UnaryExpression node, bool restrictConstants, bool recursive)
        {
            if (recursive)
                return node != null && node.NodeType == ExpressionType.Convert && IsLinkOfChain(node.Operand, restrictConstants, true);
            return node != null && node.NodeType == ExpressionType.Convert;
        }

        private static bool IsLinkOfChain(MemberExpression node, bool restrictConstants, bool recursive)
        {
            if (recursive)
                return node != null && IsLinkOfChain(node.Expression, restrictConstants, true);
            return node != null && node.Expression != null;
        }

        private static bool IsLinkOfChain(MethodCallExpression node, bool restrictConstants, bool recursive)
        {
            if (node == null || !IsAllowedMethod(node.Method))
                return false;
            if (recursive)
                return (node.Object != null && IsLinkOfChain(node.Object, restrictConstants, true)) || (node.Method.IsExtension() && IsLinkOfChain(node.Arguments[0], restrictConstants, true));
            return node.Object != null || node.Method.IsExtension();
        }

        private static bool IsLinkOfChain(BinaryExpression node, bool restrictConstants, bool recursive)
        {
            if (recursive)
                return node != null && node.NodeType == ExpressionType.ArrayIndex && IsLinkOfChain(node.Left, restrictConstants, true);
            return node != null && node.NodeType == ExpressionType.ArrayIndex;
        }

        private static bool IsAllowedMethod(MethodInfo method)
        {
            return method.DeclaringType == typeof(MutatorsHelperFunctions) || method.DeclaringType == typeof(DependenciesExtractorHelper) || method.DeclaringType == typeof(Enumerable) || method.IsIndexerGetter() || method.IsArrayIndexer();
        }
    }
}