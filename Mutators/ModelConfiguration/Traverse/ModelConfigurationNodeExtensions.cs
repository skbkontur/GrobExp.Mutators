using System;
using System.Reflection;

using JetBrains.Annotations;

namespace GrobExp.Mutators.ModelConfiguration.Traverse
{
    internal static class ModelConfigurationNodeExtensions
    {
        [CanBeNull]
        public static ModelConfigurationNode GotoEachArrayElement([NotNull] this ModelConfigurationNode node, bool create)
        {
            return node.GetChild(ModelConfigurationEdge.Each, create);
        }

        [CanBeNull]
        public static ModelConfigurationNode GotoMember([NotNull] this ModelConfigurationNode node, [NotNull] MemberInfo member, bool create)
        {
            return node.GetChild(new ModelConfigurationEdge(member), create);
        }

        [CanBeNull]
        public static ModelConfigurationNode GotoTypeConversion([NotNull] this ModelConfigurationNode node, [NotNull] Type type, bool create)
        {
            return node.GetChild(new ModelConfigurationEdge(type), create);
        }

        [CanBeNull]
        public static ModelConfigurationNode GotoIndexer([NotNull] this ModelConfigurationNode node, [NotNull] object[] parameters, bool create)
        {
            var edge = new ModelConfigurationEdge(parameters);
            var property = node.NodeType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new InvalidOperationException("Type '" + node.NodeType + "' doesn't contain indexer");

            if (!property.CanRead || !property.CanWrite)
                throw new InvalidOperationException("Type '" + node.NodeType + "' has indexer that doesn't contain either getter or setter");

            return node.GetChild(edge, create);
        }

        [CanBeNull]
        public static ModelConfigurationNode GotoArrayElement([NotNull] this ModelConfigurationNode node, int index, bool create)
        {
            return node.GetChild(new ModelConfigurationEdge(index), create);
        }

        [CanBeNull]
        private static ModelConfigurationNode GetChild([NotNull] this ModelConfigurationNode node, [NotNull] ModelConfigurationEdge edge, bool create)
        {
            if (node.children.TryGetValue(edge, out var child))
                return child;

            if (!create)
                return null;

            var childPath = node.Path.GetPathToChild(edge);
            child = new ModelConfigurationNode(node.ConfiguratorType, node.RootType, childPath.Type, node.Root, node, edge, childPath);
            node.children.Add(edge, child);

            return child;
        }
    }
}