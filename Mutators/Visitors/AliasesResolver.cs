using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    /// <summary>
    ///     Visits all nodes in a given Expression and substitutes matching aliases.
    ///     Aliases are given by key-value pairs, where values is an expression to search, and key is substitution.
    ///     Value of alias is non-strictly searched in expression
    ///     So, parameters may differ, but alias still will be resolved:
    ///     new AliasesResolver(new { (x.A, x.A) }).Visit(data.A) returns x.A (sic! parameter is replaced)
    ///     Sometimes it causes uncompilable expressions:
    ///     new AliasesResolver(new { (x.A, x.A) }).Visit(data => data.A) returns data => x.A (compilation will fail because of unknown parameter x)
    ///     This behaviour is actively used in some places, so it can't be easily fixed to be more clear.
    /// </summary>
    internal class AliasesResolver : ExpressionVisitor
    {
        public AliasesResolver(List<KeyValuePair<Expression, Expression>> aliases)
        {
            this.aliases = aliases;
            targetParameterToReplacementParameterMapping = new Dictionary<ParameterExpression, ParameterExpression>();
        }

        /// <summary>
        ///     Using this resolver on lambdas causes problems sometimes (more info about it - in summary of this AliasesResolver class).
        ///     If you are sure that you want to use it, just cast argument to Expression to invoke method with another signature.
        ///     If you are really sure that using this class with LambdaExpression is not encouraged to no purpose, feel free to remove Obsolete attribute.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        [Obsolete("Use LambdaAliasesResolver instead", true)]
        public Expression Visit(LambdaExpression node)
        {
            throw new NotImplementedException();
        }

        public override Expression Visit(Expression node)
        {
            return node.IsLinkOfChain(restrictConstants : false, recursive : false) ? SubstituteAlias(node) : base.Visit(node);
        }

        private Expression SubstituteAlias(Expression chain)
        {
            var shards = chain.SmashToSmithereens();
            int index;
            Expression replacement = null;
            // Search for longest shard, matching some alias
            for (index = shards.Length - 1; index >= 0; --index)
            {
                foreach (var pair in aliases)
                {
                    var pattern = pair.Value;
                    if (Fits(shards[index], pattern, out var parameterMappingFromTargetToPatternExpression))
                    {
                        replacement = pair.Key;
                        var replacementParameterDict = new Dictionary<(string, Type), ParameterExpression>();
                        foreach (var replacementParameter in replacement.ExtractParameters())
                        {
                            if (replacementParameterDict.ContainsKey((replacementParameter.Name, replacementParameter.Type)))
                                throw new Exception("More than one parameter with same name");
                            replacementParameterDict[(replacementParameter.Name, replacementParameter.Type)] = replacementParameter;
                        }

                        foreach (var (targetExpressionParameter, patternExpressionParameter) in parameterMappingFromTargetToPatternExpression)
                        {
                            if (!replacementParameterDict.TryGetValue((patternExpressionParameter.Name, patternExpressionParameter.Type), out var replacementParameter))
                                continue;
                            targetParameterToReplacementParameterMapping[targetExpressionParameter] = replacementParameter;
                        }

                        break;
                    }
                }

                if (replacement != null)
                    break;
            }

            if (replacement == null)
                return base.Visit(chain);
            var result = replacement;
            // Rebuild tail after substituting prefix of chain with found alias
            for (++index; index < shards.Length; ++index)
            {
                var shard = shards[index];
                switch (shard.NodeType)
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
                case ExpressionType.Coalesce:
                    result = Expression.Coalesce(result, Visit(((BinaryExpression)shard).Right));
                    break;
                default:
                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
                }
            }

            return result;
        }

        private bool Fits(Expression targetPath, Expression patternPath, out List<(ParameterExpression targetExpressionParameter, ParameterExpression patternExpressionParameter)> parameterMappingFromTargetToPatternExpression)
        {
            return ExpressionEquivalenceChecker.Equivalent(targetPath, patternPath, strictly : false, distinguishEachAndCurrent : false, parameterMappingFromFirstToSecondExpression : out parameterMappingFromTargetToPatternExpression);
        }

        private static Expression[] GetArguments(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.IsExtension() ? methodCallExpression.Arguments.Skip(1).ToArray() : methodCallExpression.Arguments.ToArray();
        }

        public IReadOnlyDictionary<ParameterExpression, ParameterExpression> TargetParameterToReplacementParameterMapping => targetParameterToReplacementParameterMapping;

        private readonly Dictionary<ParameterExpression, ParameterExpression> targetParameterToReplacementParameterMapping;
        private readonly List<KeyValuePair<Expression, Expression>> aliases;
    }
}