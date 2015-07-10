using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class ComplexMutatorsTree<TData> : MutatorsTree<TData>
    {
        public ComplexMutatorsTree(MutatorsTree<TData>[] trees)
        {
            this.trees = trees;
        }

        internal override MutatorsTree<T> Migrate<T>(ModelConfigurationNode converterTree)
        {
            return new ComplexMutatorsTree<T>(trees.Select(tree => tree.Migrate<T>(converterTree)).ToArray());
        }

        internal override MutatorsTree<TData> MigratePaths<T>(ModelConfigurationNode converterTree)
        {
            return new ComplexMutatorsTree<TData>(trees.Select(tree => tree.MigratePaths<T>(converterTree)).ToArray());
        }

        public override MutatorsTree<TData> Merge(MutatorsTree<TData> other)
        {
            return new ComplexMutatorsTree<TData>(new[] {this, other});
        }

        protected override KeyValuePair<Expression, List<KeyValuePair<int, MutatorConfiguration>>> BuildRawMutators<TValue>(Expression<Func<TData, TValue>> path)
        {
            List<KeyValuePair<int, MutatorConfiguration>> mutators = null;
            Expression abstractPath = null;
            foreach(var tree in trees)
            {
                var current = tree.GetRawMutators(path);
                if(current.Key == null)
                    continue;
                if(abstractPath != null && !ExpressionEquivalenceChecker.Equivalent(abstractPath, current.Key, false, true))
                    throw new InvalidOperationException();
                abstractPath = current.Key;
                if(mutators == null)
                    mutators = current.Value;
                else
                    mutators.AddRange(current.Value);
            }
            return new KeyValuePair<Expression, List<KeyValuePair<int, MutatorConfiguration>>>(abstractPath, mutators);
        }

        protected override KeyValuePair<Expression, List<MutatorConfiguration>> BuildMutators<TValue>(Expression<Func<TData, TValue>> path)
        {
            var rawMutators = GetRawMutators(path);
            return new KeyValuePair<Expression, List<MutatorConfiguration>>(rawMutators.Key, Canonize(rawMutators.Value));
        }

        protected override Action<TChild, ValidationResultTreeNode> BuildValidator<TChild>(Expression<Func<TData, TChild>> path)
        {
            var validators = trees.Select(tree => tree.GetValidatorInternal(path)).ToArray();
            return (child, tree) =>
                {
                    foreach(var validator in validators)
                        validator(child, tree);
                };
        }

        protected override Func<TValue, bool> BuildStaticValidator<TValue>(Expression<Func<TData, TValue>> path)
        {
            var validators = trees.Select(tree => tree.GetStaticValidator(path)).ToArray();
            return value => validators.Aggregate(true, (result, func) => result && func(value));
        }

        protected override Action<TChild> BuildTreeMutator<TChild>(Expression<Func<TData, TChild>> path)
        {
            var mutators = trees.Select(tree => tree.GetTreeMutator(path)).ToArray();
            return child =>
                {
                    foreach(var mutator in mutators)
                        mutator(child);
                };
        }

        protected override void GetAllMutators(List<KeyValuePair<Expression, MutatorConfiguration>> mutators)
        {
            foreach(var tree in trees)
                mutators.AddRange(tree.GetAllMutatorsWithPaths());
        }

        private readonly MutatorsTree<TData>[] trees;
    }
}