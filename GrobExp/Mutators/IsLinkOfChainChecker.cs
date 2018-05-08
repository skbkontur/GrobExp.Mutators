using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Visitors;

using JetBrains.Annotations;

namespace GrobExp.Mutators
{
    /// <summary>
    ///     Проверяет, является ли Expression цепочкой вызовов (а в случае recursive:false - первым звеном такой цепочки)
    /// </summary>
    public static class IsLinkOfChainChecker
    {
        public static bool IsLinkOfChain(this Expression node, bool restrictConstants, bool recursive)
        {
            return new LinkOfChainChecker(restrictConstants, recursive).IsLinkOfChain(node);
        }

        private class LinkOfChainChecker
        {
            public LinkOfChainChecker(bool restrictConstants, bool recursive)
            {
                this.restrictConstants = restrictConstants;
                this.recursive = recursive;
            }

            public bool IsLinkOfChain([CanBeNull] Expression node)
            {
                switch (node)
                {
                case ParameterExpression parameterNode:
                    return IsLinkOfChain(parameterNode);

                case ConstantExpression constantNode:
                    return IsLinkOfChain(constantNode);

                case MemberExpression memberNode:
                    return IsLinkOfChain(memberNode);

                case BinaryExpression binaryNode:
                    return IsLinkOfChain(binaryNode);

                case UnaryExpression unaryNode:
                    return IsLinkOfChain(unaryNode);

                case MethodCallExpression methodCallNode:
                    return IsLinkOfChain(methodCallNode);

                default:
                    return false;
                }
            }

            private bool IsLinkOfChain([NotNull] ConstantExpression node)
            {
                return !restrictConstants;
            }

            private bool IsLinkOfChain([NotNull] ParameterExpression node)
            {
                return true;
            }

            private bool IsLinkOfChain([NotNull] UnaryExpression node)
            {
                if (node.NodeType != ExpressionType.Convert)
                    return false;

                return IfRecursive(() => IsLinkOfChain(node.Operand));
            }

            private bool IsLinkOfChain([NotNull] MemberExpression node)
            {
                if (node.Expression == null)
                    return false;
                return IfRecursive(() => IsLinkOfChain(node.Expression));
            }

            private bool IsLinkOfChain([NotNull] MethodCallExpression node)
            {
                if (!IsAllowedMethod(node.Method))
                    return false;

                if (node.Object != null)
                    return IfRecursive(() => IsLinkOfChain(node.Object));

                if (node.Method.IsExtension())
                    return IfRecursive(() => IsLinkOfChain(node.Arguments[0]));

                return false;
            }

            private bool IsLinkOfChain([NotNull] BinaryExpression node)
            {
                if (node.NodeType != ExpressionType.ArrayIndex)
                    return false;

                return IfRecursive(() => IsLinkOfChain(node.Left));
            }

            private static bool IsAllowedMethod([NotNull] MethodInfo method)
            {
                return method.DeclaringType == typeof(MutatorsHelperFunctions)
                       || method.DeclaringType == typeof(DependenciesExtractorHelper)
                       || method.DeclaringType == typeof(Enumerable)
                       || method.IsIndexerGetter()
                       || method.IsArrayIndexer();
            }

            private bool IfRecursive([NotNull] Func<bool> recursiveCheck)
            {
                return !recursive || recursiveCheck();
            }

            private readonly bool restrictConstants;
            private readonly bool recursive;
        }
    }
}