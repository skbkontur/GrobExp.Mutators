using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    public class ModelConfigurationNode
    {
        internal ModelConfigurationNode(Type rootType, Type nodeType, ModelConfigurationNode root, ModelConfigurationNode parent, ModelConfigurationEdge edge, Expression path)
        {
            RootType = rootType;
            NodeType = nodeType;
            Root = root ?? this;
            Parent = parent;
            Edge = edge;
            Path = path;
            mutators = new List<KeyValuePair<Expression, MutatorConfiguration>>();
        }

        public static ModelConfigurationNode CreateRoot(Type type)
        {
            return new ModelConfigurationNode(type, type, null, null, null, Expression.Parameter(type, type.Name));
        }

        public void AddMutatorSmart(LambdaExpression path, MutatorConfiguration mutator)
        {
            path = (LambdaExpression)path.Simplify();
            LambdaExpression filter;
            var simplifiedPath = PathSimplifier.SimplifyPath(path, out filter);
            mutator = mutator.ResolveAliases(ExpressionAliaser.CreateAliasesResolver(simplifiedPath.Body, path.Body));
            this.Traverse(simplifiedPath.Body, true).AddMutator(path.Body, filter == null ? mutator : mutator.If(filter));
        }

        public void AddMutator(MutatorConfiguration mutator)
        {
            if(mutator.IsUncoditionalSetter())
            {
                for(var i = 0; i < mutators.Count; ++i)
                {
                    if(mutators[i].Value.IsUncoditionalSetter() && ExpressionEquivalenceChecker.Equivalent(Path, mutators[i].Key, false, false))
                    {
                        mutators[i] = new KeyValuePair<Expression, MutatorConfiguration>(Path, mutator);
                        return;
                    }
                }
            }
            mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(Path, mutator));
        }

        public void AddMutator(Expression path, MutatorConfiguration mutator)
        {
            if(mutator.IsUncoditionalSetter())
            {
                for(var i = 0; i < mutators.Count; ++i)
                {
                    if(mutators[i].Value.IsUncoditionalSetter() && ExpressionEquivalenceChecker.Equivalent(path, mutators[i].Key, false, false))
                    {
                        mutators[i] = new KeyValuePair<Expression, MutatorConfiguration>(path, mutator);
                        return;
                    }
                }
            }
            mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(path, mutator));
        }

        public override string ToString()
        {
            return this.ToPrettyString();
        }

        public Expression Path { get; private set; }
        public IEnumerable<ModelConfigurationNode> Children { get { return children.Values.Cast<ModelConfigurationNode>(); } }
        public Type NodeType { get; private set; }
        public Type RootType { get; set; }

        internal ModelConfigurationNode Root { get; private set; }
        internal ModelConfigurationNode Parent { get; private set; }
        internal ModelConfigurationEdge Edge { get; private set; }
        internal readonly List<KeyValuePair<Expression, MutatorConfiguration>> mutators;
        internal readonly Dictionary<ModelConfigurationEdge, ModelConfigurationNode> children = new Dictionary<ModelConfigurationEdge, ModelConfigurationNode>();
    }
}