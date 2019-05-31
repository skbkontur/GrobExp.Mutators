using System.Linq.Expressions;

using GrobExp.Mutators.ModelConfiguration.Traverse;

using JetBrains.Annotations;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class ModelConfigurationNodeTraversal
    {
        [CanBeNull]
        public static ModelConfigurationNode Traverse([NotNull] this ModelConfigurationNode node, [NotNull] Expression path, bool create)
        {
            return ModelConfigurationTreeTraveler.Traverse(node, path, subRoot : null, create : create).Child;
        }

        internal static bool Traverse([NotNull] this ModelConfigurationNode node, [NotNull] Expression path, [CanBeNull] ModelConfigurationNode subRoot, [CanBeNull] out ModelConfigurationNode child, bool create)
        {
            var traverseResult = ModelConfigurationTreeTraveler.Traverse(node, path, subRoot, create);
            child = traverseResult.Child;
            return traverseResult.SubRootIsVisited;
        }
    }
}