using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.ModelConfiguration;
using GrobExp.Mutators.ModelConfiguration.Traverse;

using JetBrains.Annotations;

namespace Mutators.Tests.ConfigurationTests
{
    public class NodeBuilder
    {
        public NodeBuilder([NotNull] Type nodeType)
        {
            this.nodeType = nodeType;
            children = new List<(ModelConfigurationEdge, NodeBuilder)>();
        }

        [NotNull]
        public NodeBuilder this[[NotNull] ModelConfigurationEdge edge] { set => children.Add((edge, value)); }

        public ModelConfigurationNode Build() => Build(null, null);

        [NotNull]
        private ModelConfigurationNode Build([CanBeNull] ModelConfigurationNode parent, [CanBeNull] ModelConfigurationEdge parentEdge)
        {
            var path = parent == null || parentEdge == null ? Expression.Parameter(nodeType, nodeType.Name) : parent.Path.GetPathToChild(parentEdge);
            var currentNode = new ModelConfigurationNode(null, parent?.RootType ?? nodeType, nodeType, parent?.Root, parent, parentEdge, path);
            foreach (var (edge, builder) in children)
                currentNode.children.Add(edge, builder.Build(currentNode, edge));

            return currentNode;
        }

        private readonly Type nodeType;
        private readonly List<(ModelConfigurationEdge, NodeBuilder)> children;
    }
}