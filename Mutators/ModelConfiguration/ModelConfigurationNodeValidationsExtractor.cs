using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    internal static class ModelConfigurationNodeValidationsExtractor
    {
        public static void ExtractValidationsFromConverters(this ModelConfigurationNode node, ModelConfigurationNode validationsTree)
        {
            var performer = new CompositionPerformer(node.RootType, validationsTree.RootType, node);
            node.ExtractValidationsFromConvertersInternal(validationsTree, performer);
        }

        private static void ExtractValidationsFromConvertersInternal(this ModelConfigurationNode node, ModelConfigurationNode validationsTree, CompositionPerformer performer)
        {
            foreach (var mutator in node.Mutators)
            {
                var equalsToConfiguration = mutator.Value as EqualsToConfiguration;
                if (equalsToConfiguration != null && equalsToConfiguration.Validator != null)
                {
                    var path = equalsToConfiguration.Validator.PathToNode;
                    var primaryDependencies = path.ExtractPrimaryDependencies().Select(lambda => lambda.Body);
                    var commonPath = primaryDependencies.FindLCP();
                    var validationsNode = commonPath == null ? validationsTree : validationsTree.Traverse(commonPath, true);
                    var mutatedValidator = equalsToConfiguration.Validator.Mutate(node.RootType, commonPath, performer);
                    if (mutatedValidator != null)
                        validationsNode.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(equalsToConfiguration.Validator.PathToValue.Body, mutatedValidator));
                }
            }

            foreach (var child in node.Children)
                child.ExtractValidationsFromConvertersInternal(validationsTree, performer);
        }
    }
}