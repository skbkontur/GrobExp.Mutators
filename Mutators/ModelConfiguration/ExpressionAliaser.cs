using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    internal static class ExpressionAliaser
    {
        internal static LambdaAliasesResolver CreateAliasesResolver(Expression simplifiedPath, Expression path)
        {
            var simplifiedPathShards = simplifiedPath.SmashToSmithereens();
            var pathShards = path.SmashToSmithereens();
            var i = simplifiedPathShards.Length - 1;
            var j = pathShards.Length - 1;
            while (i > 0 && j > 0)
            {
                if (!Equivalent(simplifiedPathShards[i], pathShards[j]))
                    break;
                --i;
                --j;
            }

            var simplifiedShard = simplifiedPathShards[i];
            var pathShard = pathShards[j];
            // To add alias with CurrentIndex call we need to trim simplifiedShard, 
            // because we replaced Select/Where calls with Current/Each calls from the tail of pathShard
            var cutSimplifiedShard = TrimTailToEachOrCurrent(simplifiedShard);
            return new LambdaAliasesResolver(new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(simplifiedShard, pathShard),
                    new KeyValuePair<Expression, Expression>(Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(cutSimplifiedShard.Type), cutSimplifiedShard),
                                                             Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(pathShard.Type), pathShard))
                });
        }

        private static bool Equivalent(Expression first, Expression second)
        {
            if (first.NodeType != second.NodeType)
                return false;
            switch (first.NodeType)
            {
            case ExpressionType.MemberAccess:
                return ((MemberExpression)first).Member == ((MemberExpression)second).Member;
            case ExpressionType.Call:
                return ((MethodCallExpression)first).Method == ((MethodCallExpression)second).Method;
            case ExpressionType.ArrayLength:
                return true;
            case ExpressionType.ArrayIndex:
                var firstIndex = ((BinaryExpression)first).Right;
                var secondIndex = ((BinaryExpression)second).Right;
                if (firstIndex.NodeType != ExpressionType.Constant)
                    throw new NotSupportedException(string.Format("Node type '{0}' is not supported", firstIndex.NodeType));
                if (secondIndex.NodeType != ExpressionType.Constant)
                    throw new NotSupportedException(string.Format("Node type '{0}' is not supported", secondIndex.NodeType));
                return ((ConstantExpression)firstIndex).Value == ((ConstantExpression)secondIndex).Value;
            case ExpressionType.Convert:
                return first.Type == second.Type;
            default:
                throw new NotSupportedException(string.Format("Node type '{0}' is not supported", first.NodeType));
            }
        }

        private static Expression TrimTailToEachOrCurrent(Expression exp)
        {
            var shards = exp.SmashToSmithereens();
            for (var i = shards.Length - 1; i > 0; --i)
            {
                if (shards[i].NodeType != ExpressionType.Call)
                    continue;
                var method = ((MethodCallExpression)shards[i]).Method;
                if (method.IsCurrentMethod() || method.IsEachMethod())
                    return shards[i];
            }

            return exp;
        }
    }
}