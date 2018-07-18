using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class ModelConfigurationNodeEnumeration
    {
        public static MutatorConfiguration[] GetMutators(this ModelConfigurationNode node)
        {
            return node.Mutators.Select(pair => pair.Value).ToArray();
        }

        public static MutatorWithPath[] GetMutatorsWithPath(this ModelConfigurationNode node)
        {
            return node.Mutators.Select(mutator => new MutatorWithPath
                {
                    PathToNode = node.Path,
                    PathToMutator = mutator.Key,
                    Mutator = mutator.Value
                }).ToArray();
        }

        public static void FindSubNodes(this ModelConfigurationNode node, List<ModelConfigurationNode> result)
        {
            if (node.children.Count == 0 && node.Mutators.Count > 0)
                result.Add(node);
            foreach (var child in node.Children)
                child.FindSubNodes(result);
        }

        public static ModelConfigurationNode FindKeyLeaf(this ModelConfigurationNode node)
        {
            foreach (var entry in node.children)
            {
                var edge = entry.Key;
                var child = entry.Value;
                var property = edge.Value as PropertyInfo;
                if (property != null && property.GetCustomAttributes(typeof(KeyLeafAttribute), false).Any() && child.children.Count == 0)
                    return child;
                var result = child.FindKeyLeaf();
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}