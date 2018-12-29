using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators.ModelConfiguration;

namespace GrobExp.Mutators
{
    public class ModelConfigurationNode
    {
        internal ModelConfigurationNode(Type configuratorType, Type rootType, Type nodeType, ModelConfigurationNode root, ModelConfigurationNode parent, ModelConfigurationEdge edge, Expression path)
        {
            ConfiguratorType = configuratorType;
            RootType = rootType;
            NodeType = nodeType;
            Root = root ?? this;
            Parent = parent;
            Edge = edge;
            Path = path;
            mutators = new List<KeyValuePair<Expression, MutatorConfiguration>>();
            children = new Dictionary<ModelConfigurationEdge, ModelConfigurationNode>();
        }

        public static ModelConfigurationNode CreateRoot(Type configuratorType, Type type)
        {
            return new ModelConfigurationNode(configuratorType, type, type, null, null, null, Expression.Parameter(type, type.Name));
        }

        public override string ToString()
        {
            return this.ToPrettyString();
        }

        public Expression Path { get; }

        public Type NodeType { get; }

        internal IEnumerable<ModelConfigurationNode> Children => children.Values;
        internal ICollection<KeyValuePair<Expression, MutatorConfiguration>> Mutators => mutators;
        internal Type ConfiguratorType { get; }
        internal Type RootType { get; }
        internal ModelConfigurationNode Root { get; }
        internal ModelConfigurationNode Parent { get; }
        internal ModelConfigurationEdge Edge { get; }

        internal readonly List<KeyValuePair<Expression, MutatorConfiguration>> mutators;
        internal readonly Dictionary<ModelConfigurationEdge, ModelConfigurationNode> children;
    }
}