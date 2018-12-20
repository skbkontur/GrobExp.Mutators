using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace GrobExp.Mutators.Visitors.CompositionPerforming
{
    public static class IsSimpleLinkOfChainChecker
    {
        public static bool IsSimpleLinkOfChain([CanBeNull] Expression node, [CanBeNull] out Type type)
        {
            type = null;
            if (node == null)
                return false;

            switch (node)
            {
            case ParameterExpression parameterExpression:
                type = parameterExpression.Type;
                return true;

            case MemberExpression memberExpression:
                return IsSimpleLinkOfChain(memberExpression, out type);

            case BinaryExpression binaryExpression:
                return binaryExpression.NodeType == ExpressionType.ArrayIndex && IsSimpleLinkOfChain(binaryExpression.Left, out type);

            case MethodCallExpression methodCallExpression:
                return IsSimpleLinkOfChain(methodCallExpression, out type);

            default:
                return false;
            }
        }

        private static bool IsSimpleLinkOfChain([NotNull] MethodCallExpression node, out Type type)
        {
            type = null;
            return allowedStaticMethods.Any(checker => checker(node.Method)) && IsSimpleLinkOfChain(node.Arguments.First(), out type)
                   || node.Method.IsIndexerGetter() && IsSimpleLinkOfChain(node.Object, out type);
        }

        private static bool IsSimpleLinkOfChain([NotNull] MemberExpression node, out Type type)
        {
            type = null;
            return node.Member != stringLengthProperty && IsSimpleLinkOfChain(node.Expression, out type);
        }

        private static readonly Func<MethodInfo, bool>[] allowedStaticMethods =
            {
                MutatorsHelperFunctions.IsCurrentMethod,
                MutatorsHelperFunctions.IsEachMethod,
                MutatorsHelperFunctions.IsTemplateIndexMethod,
                MutatorsHelperFunctions.IsWhereMethod,
            };

        private static readonly MemberInfo stringLengthProperty = ((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member;
    }
}