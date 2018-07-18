using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public class AbstractPathResolver : ExpressionVisitor
    {
        public AbstractPathResolver(List<PathPrefix> pathPrefixes, bool takeParameter)
        {
            this.pathPrefixes = pathPrefixes;
            this.takeParameter = takeParameter;
        }

        public Expression Resolve(Expression expression)
        {
            return Visit(expression);
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
                return null;
            if (IsSimpleLinkOfChain(node))
                return ResolveAbstractPath(node);
            if (node.NodeType == ExpressionType.Call && ((MethodCallExpression)node).Method.IsCurrentIndexMethod())
                return ResolveCurrentIndex((MethodCallExpression)node);
            return base.Visit(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            foreach (var parameter in lambda.Parameters)
                localParameters.Add(parameter);
            var res = base.VisitLambda(lambda);
            foreach (var parameter in lambda.Parameters)
                localParameters.Remove(parameter);
            return res;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach (var variable in node.Variables)
                localParameters.Add(variable);
            var res = base.VisitBlock(node);
            foreach (var variable in node.Variables)
                localParameters.Remove(variable);
            return res;
        }

        private static bool IsSimpleLinkOfChain(MethodCallExpression node)
        {
            return node != null && (((node.Method.IsCurrentMethod() || node.Method.IsEachMethod() || node.Method.IsTemplateIndexMethod()) && IsSimpleLinkOfChain(node.Arguments[0]))
                                    || ((node.Method.IsIndexerGetter()) && (IsSimpleLinkOfChain(node.Object))));
        }

        private static bool IsSimpleLinkOfChain(MemberExpression node)
        {
            return node != null && node.Member != stringLengthProperty && IsSimpleLinkOfChain(node.Expression);
        }

        private static bool IsSimpleLinkOfChain(Expression node)
        {
            return node != null && (node.NodeType == ExpressionType.Parameter || IsSimpleLinkOfChain(node as MemberExpression) || node.NodeType == ExpressionType.ArrayIndex || IsSimpleLinkOfChain(node as MethodCallExpression));
        }

        private Expression ResolveCurrentIndex(MethodCallExpression node)
        {
            var resolved = ResolveAbstractPath(node.Arguments.Single());
            return resolved.NodeType == ExpressionType.Parameter ? pathPrefixes.First(prefix => prefix.Parameter == resolved).Index : base.Visit(node);
        }

        private Expression ResolveAbstractPath(Expression abstractPath)
        {
            var abstractPathShards = abstractPath.SmashToSmithereens();
            var paramIndex = 0;
            var pathShards = pathPrefixes[paramIndex].Path.SmashToSmithereens();
            var i = 0;
            var j = 0;
            while (i < abstractPathShards.Length)
            {
                var abstractShard = abstractPathShards[i];
                if (j >= pathShards.Length)
                {
                    if (paramIndex >= pathPrefixes.Count - 1)
                        break;
                    pathShards = pathPrefixes[++paramIndex].Path.SmashToSmithereens();
                    j = 1;
                }
                else
                {
                    var end = true;
                    var shard = pathShards[j];
                    switch (shard.NodeType)
                    {
                    case ExpressionType.Parameter:
                        if (shard.Type == abstractShard.Type && abstractShard.NodeType == ExpressionType.Parameter && !localParameters.Contains((ParameterExpression)abstractShard))
                            end = false;
                        break;
                    case ExpressionType.MemberAccess:
                        if (abstractShard.NodeType == ExpressionType.MemberAccess && ((MemberExpression)shard).Member == ((MemberExpression)abstractShard).Member)
                            end = false;
                        break;
                    case ExpressionType.ArrayIndex:
                        if (abstractShard.NodeType == ExpressionType.Call && (((MethodCallExpression)abstractShard).Method.IsCurrentMethod() || ((MethodCallExpression)abstractShard).Method.IsEachMethod()))
                            end = false;
                        if (abstractShard.NodeType == ExpressionType.ArrayIndex)
                        {
                            var shardIndex = GetIndex(((BinaryExpression)shard).Right);
                            var abstractShardIndex = GetIndex(((BinaryExpression)abstractShard).Right);
                            if (shardIndex == abstractShardIndex)
                                end = false;
                        }

                        break;
                    case ExpressionType.Call:
                        if (((MethodCallExpression)shard).Method.IsIndexerGetter())
                        {
                            if (abstractShard.NodeType == ExpressionType.Call && (((MethodCallExpression)abstractShard).Method.IsCurrentMethod() || ((MethodCallExpression)abstractShard).Method.IsEachMethod()))
                            {
                                end = false;
                                ++i;
                                if (i >= abstractPathShards.Length || abstractPathShards[i].NodeType != ExpressionType.MemberAccess || (((MemberExpression)abstractPathShards[i])).Member.Name != "Value")
                                    throw new InvalidOperationException();
                            }

                            if (abstractShard.NodeType == ExpressionType.Call)
                            {
                                var methodCallExpression = (MethodCallExpression)abstractShard;
                                if (methodCallExpression.Method.IsIndexerGetter())
                                {
                                    var shardArgs = ((MethodCallExpression)shard).Arguments.ToArray();
                                    var abstractShardArgs = methodCallExpression.Arguments.ToArray();
                                    if (shardArgs.Length == abstractShardArgs.Length)
                                        if (!shardArgs.Where((t, k) => ((ConstantExpression)t).Value != ((ConstantExpression)abstractShardArgs[k]).Value).Any())
                                            end = false;
                                }
                            }
                        }
                        else if (abstractShard.NodeType == ExpressionType.Call)
                        {
                            var method = ((MethodCallExpression)shard).Method;
                            var abstractMethod = ((MethodCallExpression)abstractShard).Method;
                            if ((abstractMethod == method) || (abstractMethod.IsCurrentMethod() && (method.IsEachMethod() || method.IsTemplateIndexMethod())))
                                end = false;
                        }

                        break;
                    }

                    if (end) break;
                    ++j;
                    ++i;
                }
            }

            if (i == 0) return base.Visit(abstractPath);
            var result = j == pathShards.Length ? (takeParameter ? pathPrefixes[paramIndex].Parameter : pathPrefixes[paramIndex].Path) : pathShards[j - 1];
            while (i < abstractPathShards.Length)
            {
                var abstractShard = abstractPathShards[i];
                switch (abstractShard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    result = Expression.MakeMemberAccess(result, ((MemberExpression)abstractShard).Member);
                    break;
                case ExpressionType.ArrayIndex:
                    result = Expression.MakeBinary(ExpressionType.ArrayIndex, result, Visit(((BinaryExpression)abstractShard).Right));
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)abstractShard;
                    result = methodCallExpression.Method.IsStatic
                                 ? Expression.Call(methodCallExpression.Method, new[] {result}.Concat(methodCallExpression.Arguments.Skip(1)))
                                 : Expression.Call(result, methodCallExpression.Method, methodCallExpression.Arguments);
                    break;
                case ExpressionType.Convert:
                    result = Expression.Convert(result, abstractShard.Type);
                    break;
                default:
                    throw new NotSupportedException("Node type '" + abstractShard.NodeType + "' is not supported");
                }

                ++i;
            }

            return result;
        }

        private static int GetIndex(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
                return (int)((ConstantExpression)exp).Value;
            return Expression.Lambda<Func<int>>(Expression.Convert(exp, typeof(int))).Compile()();
        }

        private readonly HashSet<ParameterExpression> localParameters = new HashSet<ParameterExpression>();

        private readonly bool takeParameter;

        private readonly List<PathPrefix> pathPrefixes;

        // ReSharper disable StaticFieldInGenericType
        private static readonly PropertyInfo stringLengthProperty = (PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member;
        // ReSharper restore StaticFieldInGenericType
    }
}