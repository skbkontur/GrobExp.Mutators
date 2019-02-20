using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    internal static class ModelConfigurationNodeModification
    {
        public static void AddMutatorSmart(this ModelConfigurationNode node, LambdaExpression path, MutatorConfiguration mutator, ConfiguratorReporter reporter = null)
        {
            path = (LambdaExpression)path.Simplify();
            var simplifiedPath = PathSimplifier.SimplifyPath(path, out var filter);
            mutator = mutator.ResolveAliases(ExpressionAliaser.CreateAliasesResolver(simplifiedPath.Body, path.Body));
            var mutatorToAdd = filter == null ? mutator : mutator.If(filter);
            reporter?.Report(simplifiedPath, path, mutatorToAdd);
            node.Traverse(simplifiedPath.Body, true).AddMutator(path.Body, mutatorToAdd);
        }

        public static void AddMutator(this ModelConfigurationNode node, MutatorConfiguration mutator)
        {
            if (mutator.IsUncoditionalSetter())
            {
                for (var i = 0; i < node.Mutators.Count; ++i)
                {
                    if (node.mutators[i].Value.IsUncoditionalSetter() && ExpressionEquivalenceChecker.Equivalent(node.Path, node.mutators[i].Key, false, false))
                    {
                        node.mutators[i] = new KeyValuePair<Expression, MutatorConfiguration>(node.Path, mutator);
                        return;
                    }
                }
            }

            node.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(node.Path, mutator));
        }

        public static void AddMutator(this ModelConfigurationNode node, Expression path, MutatorConfiguration mutator)
        {
            if (mutator.IsUncoditionalSetter())
            {
                for (var i = 0; i < node.mutators.Count; ++i)
                {
                    if (node.mutators[i].Value.IsUncoditionalSetter() && ExpressionEquivalenceChecker.Equivalent(path, node.mutators[i].Key, false, false))
                    {
                        node.mutators[i] = new KeyValuePair<Expression, MutatorConfiguration>(path, mutator);
                        return;
                    }
                }
            }

            node.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(path, mutator));
        }
    }
}