using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.ModelConfiguration;

using Mutators.Tests.Helpers;

namespace Mutators.Tests.ConfigurationTests
{
    public class NodeBuilder
    {
        public NodeBuilder(Type nodeType)
        {
            this.nodeType = nodeType;
            children = new List<(ModelConfigurationEdge, NodeBuilder)>();
        }

        public NodeBuilder this[ModelConfigurationEdge edge] { set => children.Add((edge, value)); }

        public ModelConfigurationNode Build() => Build(null, null);

        private ModelConfigurationNode Build(ModelConfigurationNode parent, ModelConfigurationEdge parentEdge)
        {
            var path = parent == null ? Expression.Parameter(nodeType, nodeType.Name) : parent.Path.Goto(parentEdge);
            var currentNode = new ModelConfigurationNode(null, parent?.RootType ?? nodeType, nodeType, parent?.Root, parent, parentEdge, path);
            foreach (var (edge, builder) in children)
            {
                currentNode.children.Add(edge, builder.Build(currentNode, edge));
            }
            return currentNode;
        }

        private readonly Type nodeType;
        private readonly List<(ModelConfigurationEdge, NodeBuilder)> children;
    }
}