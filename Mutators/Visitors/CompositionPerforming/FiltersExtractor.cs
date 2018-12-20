using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace GrobExp.Mutators.Visitors.CompositionPerforming
{
    public static class FiltersExtractor
    {
        [NotNull]
        public static Expression CleanFilters([NotNull] this Expression node, [NotNull] out LambdaExpression[] filters)
        {
            var foundFilters = new List<LambdaExpression>();
            var shards = node.SmashToSmithereens();
            var result = shards[0];
            var i = 0;
            while (i < shards.Length - 1)
            {
                ++i;
                var shard = shards[i];
                switch (shard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    result = Expression.MakeMemberAccess(result, ((MemberExpression)shard).Member);
                    break;
                case ExpressionType.ArrayIndex:
                    result = Expression.ArrayIndex(result, ((BinaryExpression)shard).Right);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)shard;
                    if (methodCallExpression.Method.IsWhereMethod() && (i == shards.Length - 1 || IsEachOrCurrentMethodCall(shards[i + 1])))
                    {
                        if (i == shards.Length - 1)
                            foundFilters.Add(Expression.Lambda(Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(result.Type.GetItemType()), result), (ParameterExpression)shards[0]).Merge((LambdaExpression)methodCallExpression.Arguments[1]));
                        else
                        {
                            result = Expression.Call(((MethodCallExpression)shards[i + 1]).Method, result);
                            foundFilters.Add(Expression.Lambda(result, (ParameterExpression)shards[0]).Merge((LambdaExpression)methodCallExpression.Arguments[1]));
                            ++i;
                        }
                    }
                    else
                    {
                        if (IsEachOrCurrentMethodCall(methodCallExpression))
                            foundFilters.Add(null);
                        result = methodCallExpression.Method.IsStatic
                                     ? Expression.Call(methodCallExpression.Method, new[] {result}.Concat(methodCallExpression.Arguments.Skip(1)))
                                     : Expression.Call(result, methodCallExpression.Method, methodCallExpression.Arguments);
                    }

                    break;
                default:
                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
                }
            }

            filters = foundFilters.ToArray();
            return result;
        }

        private static bool IsEachOrCurrentMethodCall([CanBeNull] Expression expression)
        {
            if (!(expression is MethodCallExpression methodCallExpression))
                return false;

            var methodInfo = methodCallExpression.Method;
            return methodInfo.IsCurrentMethod() || methodInfo.IsEachMethod();
        }
    }
}