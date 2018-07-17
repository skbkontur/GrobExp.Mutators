using System;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class PathSimplifier
    {
        /// <summary>
        ///     Выкидываем вызовы Select и Where, заменяя их на на Current.
        ///     Из всех Where строим жирную лямбду и скидываем её в filter.
        ///     <code>
        ///     qxx.Data.Where(x => x.EndsWith("GRobas")).Select(x => x.ToArray()).Where(x => x.Length > 0).Each().Current() ->
        ///         qxx.Data.Current().ToArray().Current()
        ///         filter = qxx.Data.Current().EndsWith("GRobas") && qxx.Data.Current().ToArray().Length > 0
        /// </code>
        /// </summary>
        public static LambdaExpression SimplifyPath(LambdaExpression path, out LambdaExpression filter)
        {
            filter = null;
            var shards = path.Body.SmashToSmithereens();
            int i;
            for (i = 0; i < shards.Length; ++i)
            {
                if (shards[i].NodeType == ExpressionType.Call && ((MethodCallExpression)shards[i]).Method.DeclaringType == typeof(Enumerable))
                    break;
            }

            if (i >= shards.Length)
                return path;
            var result = shards[i - 1];
            var currents = 0;
            for (; i < shards.Length; ++i)
            {
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
                    var method = methodCallExpression.Method;
                    if (method.DeclaringType == typeof(Enumerable))
                    {
                        switch (method.Name)
                        {
                        case "Select":
                            // Substitute call to Select method with Current
                            // arr.Select(x => x.y) -> arr.Current().y
                            var selector = (LambdaExpression)methodCallExpression.Arguments[1];
                            result =
                                Expression.Lambda(
                                              Expression.Call(
                                                  MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(result.Type.GetItemType()), result), path.Parameters)
                                          .Merge(selector)
                                          .Body;
                            ++currents;
                            break;
                        case "Where":
                            // Remove call to Where, saving it to filter
                            // arr.Where(x => x.y > 0) ->
                            // result := arr
                            // filter := filter && (arr.Current().y > 0)
                            //
                            // If we had Select call replaced by Current, new Current call is not added
                            // arr.Select(x => x.y).Where(y => y.z > 0) -> arr.Current().y.Where(y => y.z > 0) ->
                            // result = arr.Current().y
                            // filter := filter && arr.Current().y.z > 0
                            var predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                            var callExpression = result.Type == predicate.Parameters[0].Type
                                                     ? result
                                                     : Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(result.Type.GetItemType()), result);
                            var currentFilter = Expression.Lambda(callExpression, path.Parameters).Merge(predicate);
                            filter = filter == null ? currentFilter : filter.AndAlso(currentFilter, false);
                            break;
                        default:
                            throw new NotSupportedException(string.Format("Method '{0}' is not supported", method));
                        }
                    }
                    else if (method.DeclaringType == typeof(MutatorsHelperFunctions))
                    {
                        switch (method.Name)
                        {
                        case "Current":
                        case "Each":
                            // Remove Each/Current call if it is added before by processing of Select/Where calls.
                            --currents;
                            if (currents < 0)
                            {
                                result = Expression.Call(method.GetGenericMethodDefinition().MakeGenericMethod(result.Type.GetItemType()), result);
                                ++currents;
                            }

                            break;
                        default:
                            throw new NotSupportedException(string.Format("Method '{0}' is not supported", method));
                        }
                    }
                    else
                        throw new NotSupportedException(string.Format("Method '{0}' is not supported", method));

                    break;
                case ExpressionType.ArrayLength:
                    result = Expression.ArrayLength(result);
                    break;
                case ExpressionType.Convert:
                    result = Expression.Convert(result, shard.Type);
                    break;
                default:
                    throw new NotSupportedException(string.Format("Node type '{0}' is not valid at this point", shard.NodeType));
                }
            }

            return Expression.Lambda(result, path.Parameters);
        }
    }
}