using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class ModelConfigurationNodeArraysExtraction
    {
        public static Expression GetAlienArray(this ModelConfigurationNode node)
        {
            var arrays = node.GetArrays();
            Expression result;
            if(!arrays.TryGetValue(node.RootType, out result))
                return null;
            return ExpressionEquivalenceChecker.Equivalent(Expression.Lambda(result, result.ExtractParameters()).ExtractPrimaryDependencies()[0].Body, node.Path, false, true) ? null : result;
        }

        public static Dictionary<Type, Expression> GetArrays(this ModelConfigurationNode node)
        {
            var arrays = new Dictionary<Type, List<Expression>>();
            node.GetArrays(node.Path, arrays);
            return arrays
                .Where(pair => pair.Value.Count > 0)
                .ToDictionary(pair => pair.Key,
                    pair => pair.Value
                        .GroupBy(expression => new ExpressionWrapper(expression, strictly : false))
                        .Select(grouping =>
                        {
                            var exp = grouping.First();
                            return exp.NodeType == ExpressionType.Call ? ((MethodCallExpression)exp).Arguments[0] : exp;
                        })
                        .FirstOrDefault());
        }

        /// <summary>
        ///     Runs <see cref="ArraysExtractor" /> for all mutators in all nodes in the sub-tree and
        ///     saves to <paramref name="arrays" /> all expressions with level (count of Each() and Current() calls)
        ///     matching the level of <paramref name="path" />.
        /// </summary>
        private static void GetArrays(this ModelConfigurationNode node, Expression path, Dictionary<Type, List<Expression>> arrays)
        {
            if(node.Mutators != null && node.Mutators.Count > 0)
            {
                var shards = path.SmashToSmithereens();
                var level = 1;
                foreach(var shard in shards)
                {
                    if(shard.NodeType == ExpressionType.Call && ((MethodCallExpression)shard).Method.IsEachMethod())
                        ++level;
                }

                var list = new List<Dictionary<Type, List<Expression>>>();
                var arraysExtractor = new ArraysExtractor(list);

                foreach(var mutator in node.Mutators)
                    mutator.Value.GetArrays(arraysExtractor);

                if(list.Count > level)
                {
                    var dict = list[level];
                    foreach(var pair in dict)
                    {
                        List<Expression> lizd;
                        if(!arrays.TryGetValue(pair.Key, out lizd))
                            arrays.Add(pair.Key, lizd = new List<Expression>());
                        lizd.AddRange(pair.Value);
                    }
                }
            }
            foreach(var child in node.children.Values)
                child.GetArrays(path, arrays);
        }
    }
}