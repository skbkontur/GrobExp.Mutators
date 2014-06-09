using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators.Aggregators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class SimpleMutatorsTree<TData> : MutatorsTree<TData>
    {
        public SimpleMutatorsTree(ModelConfigurationNode tree, IPathFormatter pathFormatter, IPathFormatterCollection pathFormatterCollection, int priority)
        {
            this.tree = tree;
            this.pathFormatterCollection = pathFormatterCollection;
            this.pathFormatter = pathFormatter;
            this.priority = priority;
        }

        public override MutatorsTree<TData> Merge(MutatorsTree<TData> other)
        {
            return new ComplexMutatorsTree<TData>(new[] {this, other});
        }

        internal override MutatorsTree<T> Migrate<T>(ModelConfigurationNode converterTree)
        {
            var migratedTree = ModelConfigurationNode.CreateRoot(typeof(T));
            tree.Migrate(typeof(T), migratedTree, converterTree);
            return new SimpleMutatorsTree<T>(migratedTree, pathFormatterCollection.GetPathFormatter<T>(), pathFormatterCollection, priority);
        }

        internal override MutatorsTree<TData> MigratePaths<T>(ModelConfigurationNode converterTree)
        {
            var performer = new CompositionPerformer(typeof(TData), typeof(T), converterTree, new List<KeyValuePair<Expression, Expression>>());
            var resolver = new AliasesResolver(ExtractAliases(converterTree, performer), false);
            return new SimpleMutatorsTree<TData>(tree, new PathFormatterWrapper(pathFormatterCollection.GetPathFormatter<T>(), pathFormatterCollection.GetPathFormatter<TData>(), converterTree, performer, resolver), pathFormatterCollection, priority);
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

        protected override void GetAllMutators(List<MutatorConfiguration> mutators)
        {
            var subNodes = new List<ModelConfigurationNode>();
            tree.FindSubNodes(subNodes);
            foreach(var node in subNodes)
                mutators.AddRange(node.GetMutators());
        }

        private static List<KeyValuePair<Expression, Expression>> ExtractAliases(ModelConfigurationNode converterTree, CompositionPerformer performer)
        {
            var aliases = new List<KeyValuePair<Expression, Expression>>();
            ExtractAliases(converterTree, performer, aliases);
            return aliases;
        }

        private static bool IsEachOrCurrent(Expression node)
        {
            if(node.NodeType != ExpressionType.Call) return false;
            var method = ((MethodCallExpression)node).Method;
            return method.IsEachMethod() || method.IsCurrentMethod();
        }

        private static void ExtractAliases(ModelConfigurationNode node, CompositionPerformer performer, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if(node.GetMutators().Any())
            {
                var conditionalSetters = performer.GetConditionalSetters(node.Path);
                if(conditionalSetters != null && conditionalSetters.Count == 1)
                {
                    var setter = conditionalSetters.Single();
                    if(setter.Value == null)
                    {
                        var chains = setter.Key.CutToChains(true, true);
                        if(chains.Length == 1)
                        {
                            var chain = chains.Single();
                            aliases.Add(new KeyValuePair<Expression, Expression>(node.Path, chain));
                            if(IsEachOrCurrent(node.Path))
                                aliases.Add(new KeyValuePair<Expression, Expression>(Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(node.Path.Type), node.Path), Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(chain.Type), chain)));
                        }
                    }
                }
            }
            else if(IsEachOrCurrent(node.Path))
            {
                var conditionalSetters = performer.GetConditionalSetters(((MethodCallExpression)node.Path).Arguments.Single());
                if (conditionalSetters != null && conditionalSetters.Count == 1)
                {
                    var setter = conditionalSetters.Single();
                    if (setter.Value == null)
                    {
                        var chains = setter.Key.CutToChains(true, true);
                        if (chains.Length == 1)
                        {
                            var chain = chains.Single();
                            chain = Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(chain.Type.GetItemType()), chain);
                            aliases.Add(new KeyValuePair<Expression, Expression>(Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(node.Path.Type), node.Path), Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(chain.Type), chain)));
                        }
                    }
                }
            }
            foreach(var child in node.Children)
                ExtractAliases(child, performer, aliases);
        }

        private readonly IPathFormatter pathFormatter;
        private readonly int priority;
        private readonly ModelConfigurationNode tree;
        private readonly IPathFormatterCollection pathFormatterCollection;
    }
}