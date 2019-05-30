using System.Linq.Expressions;

using GrobExp.Mutators.ModelConfiguration.Traverse;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class ModelConfigurationNodeTraversal
    {
        public static ModelConfigurationNode Traverse(this ModelConfigurationNode node, Expression path, bool create)
        {
            return ModelConfigurationTreeTraveler.Traverse(node, path, subRoot : null, create : create).Child;
        }

        internal static bool Traverse(this ModelConfigurationNode node, Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create)
        {
            var traverseResult = ModelConfigurationTreeTraveler.Traverse(node, path, subRoot, create);
            child = traverseResult.Child;
            return traverseResult.SubRootIsVisited;
        }
    }
}