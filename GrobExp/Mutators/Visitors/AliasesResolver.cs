using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class AliasesResolver : ExpressionVisitor
    {
        public AliasesResolver(List<KeyValuePair<Expression, Expression>> aliases, bool strictly)
        {
            this.aliases = aliases;
            this.strictly = strictly;
        }

        public override Expression Visit(Expression node)
        {
            return node.IsLinkOfChain(false, false) ? SubstituteAlias(node) : base.Visit(node);
        }

        private Expression SubstituteAlias(Expression chain)
        {
            var shards = chain.SmashToSmithereens();
            int index;
            Expression alias = null;
            for(index = shards.Length - 1; index >= 0; --index)
            {
                foreach(var pair in aliases)
                {
                    if(Fits(shards[index], pair.Value))
                    {
                        alias = pair.Key;
                        break;
                    }
                }
                if(alias != null)
                    break;
            }
            if(alias == null)
                return base.Visit(chain);
            Expression result = alias;
            for(++index; index < shards.Length; ++index)
            {
                var shard = shards[index];
                switch(shard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    result = Expression.MakeMemberAccess(result, ((MemberExpression)shard).Member);
                    break;
                case ExpressionType.ArrayIndex:
                    result = Expression.ArrayIndex(result, Visit(((BinaryExpression)shard).Right));
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)shard;
                    Expression[] arguments = GetArguments(methodCallExpression).Select(Visit).ToArray();
                    result = methodCallExpression.Method.IsExtension()
                                 ? Expression.Call(methodCallExpression.Method, new[] {result}.Concat(arguments))
                                 : Expression.Call(result, methodCallExpression.Method, arguments);
                    break;
                case ExpressionType.Convert:
                    result = Expression.Convert(result, shard.Type);
                    break;
                default:
                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
                }
            }
            return result;
        }

        private bool Fits(Expression abstractPath, Expression aliasPath)
        {
            var first = replacer.Visit(abstractPath);
            var second = replacer.Visit(aliasPath);
            return ExpressionEquivalenceChecker.Equivalent(first, second, strictly);
        }

        private static Expression[] GetArguments(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.IsExtension() ? methodCallExpression.Arguments.Skip(1).ToArray() : methodCallExpression.Arguments.ToArray();
        }

        private readonly MethodReplacer replacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);

//        private static bool IsSimpleLinkOfChain(MethodCallExpression node)
//        {
//            return node != null && ((node.Object != null && IsSimpleLinkOfChain(node.Object)) || (node.Method.IsExtension() && IsSimpleLinkOfChain(node.Arguments[0])));
//        }
//
//        private static bool IsSimpleLinkOfChain(MemberExpression node)
//        {
//            return node != null && node.Member != stringLengthProperty && IsSimpleLinkOfChain(node.Expression);
//        }
//
//        private static bool IsSimpleLinkOfChain(Expression node)
//        {
//            return node != null && (node.NodeType == ExpressionType.Parameter || IsSimpleLinkOfChain(node as MemberExpression) || node.NodeType == ExpressionType.ArrayIndex || IsSimpleLinkOfChain(node as MethodCallExpression));
//        }

        private readonly List<KeyValuePair<Expression, Expression>> aliases;
        private readonly bool strictly;
    }
}