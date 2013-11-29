using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators.Aggregators;

namespace GrobExp.Mutators
{
    public class SimpleMutatorsTree<TData> : MutatorsTree<TData>
    {
        public SimpleMutatorsTree(ModelConfigurationNode tree, IPathFormatterCollection pathFormatterCollection, int priority)
        {
            this.tree = tree;
            this.pathFormatterCollection = pathFormatterCollection;
            pathFormatter = pathFormatterCollection.GetPathFormatter<TData>();
            this.priority = priority;
        }

        public override MutatorsTree<T> Migrate<T>(ModelConfigurationNode converterTree)
        {
            var migratedTree = ModelConfigurationNode.CreateRoot(typeof(T));
            tree.Migrate(typeof(T), migratedTree, converterTree);
            return new SimpleMutatorsTree<T>(migratedTree, pathFormatterCollection, priority);
        }

        public override MutatorsTree<TData> Merge(MutatorsTree<TData> other)
        {
            return new ComplexMutatorsTree<TData>(new[] {this, other});
        }

        protected override KeyValuePair<Expression, List<KeyValuePair<int, MutatorConfiguration>>> BuildRawMutators<TValue>(Expression<Func<TData, TValue>> path)
        {
            var node = tree.Traverse(path.Body, false);
            if(node == null)
                return default(KeyValuePair<Expression, List<KeyValuePair<int, MutatorConfiguration>>>);
            IEnumerable<MutatorConfiguration> mutators;
            var alienArray = node.NodeType.IsArray ? node.GetAlienArray() : null;
            if(alienArray == null)
                mutators = node.GetMutators();
            else
            {
                var setSourceArrayConfiguration = SetSourceArrayConfiguration.Create(Expression.Lambda(alienArray, alienArray.ExtractParameters()));
                mutators = node.GetMutators().Concat(new[] {setSourceArrayConfiguration});
            }
            return new KeyValuePair<Expression, List<KeyValuePair<int, MutatorConfiguration>>>(node.Path, mutators.Select(mutator => new KeyValuePair<int, MutatorConfiguration>(priority, mutator)).ToList());
        }

        protected override KeyValuePair<Expression, List<MutatorConfiguration>> BuildMutators<TValue>(Expression<Func<TData, TValue>> path)
        {
            var rawMutators = GetRawMutators(path);
            return new KeyValuePair<Expression, List<MutatorConfiguration>>(rawMutators.Key, Canonize(rawMutators.Value));
        }

        protected override Action<TChild, ValidationResultTreeNode> BuildValidator<TChild>(Expression<Func<TData, TChild>> path)
        {
            Expression<Action<TChild, ValidationResultTreeNode, int>> validator = null;
            var node = tree.Traverse(path.Body, false);
            if(node != null)
                validator = (Expression<Action<TChild, ValidationResultTreeNode, int>>)node.BuildTreeValidator(pathFormatter);
            if(validator == null)
                return (child, validationResultTree) => { };
            var compiledValidator = LambdaCompiler.Compile(validator, CompilerOptions.All);
            return (child, validationResultTree) => compiledValidator(child, validationResultTree, priority);
        }

        protected override Func<TValue, bool> BuildStaticValidator<TValue>(Expression<Func<TData, TValue>> path)
        {
            Expression<Func<TValue, List<ValidationResult>>> validator = null;
            var node = tree.Traverse(path.Body, false);
            if(node != null)
                validator = (Expression<Func<TValue, List<ValidationResult>>>)node.BuildStaticNodeValidator();
            if(validator == null)
                return value => true;
            var compiledValidator = LambdaCompiler.Compile(validator, CompilerOptions.All);
            return value => compiledValidator(value).All(validationResult => validationResult.Type != ValidationResultType.Error);
        }

        protected override Action<TChild> BuildTreeMutator<TChild>(Expression<Func<TData, TChild>> path)
        {
            Expression<Action<TChild>> mutator = null;
            var node = tree.Traverse(path.Body, false);
            if(node != null)
                mutator = (Expression<Action<TChild>>)node.BuildTreeMutator();
            if(mutator == null)
                return child => { };
            return LambdaCompiler.Compile(mutator, CompilerOptions.All);
        }

        private readonly IPathFormatter pathFormatter;
        private readonly int priority;
        private readonly ModelConfigurationNode tree;
        private readonly IPathFormatterCollection pathFormatterCollection;
    }
}