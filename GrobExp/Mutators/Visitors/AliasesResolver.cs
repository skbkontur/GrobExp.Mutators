using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    /// <summary>
    ///     Visits all nodes in a given Expression and substitutes matching aliases.
    ///     Aliases are given by key-value pairs, where values is an expression to search, and key is substitution.
    /// </summary>
    public class AliasesResolver : ExpressionVisitor
    {
        public AliasesResolver(List<KeyValuePair<Expression, Expression>> aliases)
        {
            this.aliases = aliases;
        }

        public override Expression Visit(Expression node)
        {
            return node.IsLinkOfChain(restrictConstants : false, recursive : false) ? SubstituteAlias(node) : base.Visit(node);
        }

        private Expression SubstituteAlias(Expression chain)
        {
            var shards = chain.SmashToSmithereens();
            int index;
            Expression alias = null;
            // Search for longest shard, matching some alias
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
            var result = alias;
            // Rebuild tail after substituting prefix of chain with found alias
            for(++index; index < shards.Length; ++index)
            {
                var shard = shards[index];
                switch(shard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    result = Expression.MakeMemberAccess(result, ((MemberExpression)shard).Member);
                    break;
                case ExpressionType.ArrayIndex:
                    // Resolve aliases for 'index' expression 
                    result = Expression.ArrayIndex(result, Visit(((BinaryExpression)shard).Right));
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)shard;
                    // Resolve aliases for arguments' expressions
                    var arguments = GetArguments(methodCallExpression).Select(Visit).ToArray();
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
            return ExpressionEquivalenceChecker.Equivalent(abstractPath, aliasPath, strictly : false, distinguishEachAndCurrent : false);
        }

        private static Expression[] GetArguments(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.IsExtension() ? methodCallExpression.Arguments.Skip(1).ToArray() : methodCallExpression.Arguments.ToArray();
        }

        private readonly List<KeyValuePair<Expression, Expression>> aliases;
    }
}