using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class PathFormatterWrapper : IPathFormatter
    {
        public PathFormatterWrapper(IPathFormatter pathFormatter, IPathFormatter basePathFormatter, ModelConfigurationNode converterTree, CompositionPerformer performer, AliasesResolver resolver)
        {
            this.pathFormatter = pathFormatter;
            this.basePathFormatter = basePathFormatter;
            this.converterTree = converterTree;
            this.performer = performer;
            this.resolver = resolver;
        }

        public Expression GetFormattedPath(Expression[] paths)
        {
            var migratedPaths = new List<Expression>();
            foreach(var path in paths)
            {
                var conditionalSetters = performer.GetConditionalSetters(path);
                if(conditionalSetters != null)
                    migratedPaths.AddRange(conditionalSetters.SelectMany(setter => setter.Key.CutToChains(true, true)));
                else
                {
                    var performedPath = performer.Perform(path);
                    if(performedPath.NodeType == ExpressionType.Constant && ((ConstantExpression)performedPath).Value == null)
                    {
                        var primaryDependencies = Expression.Lambda(path, path.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body).ToArray();
                        if(primaryDependencies.Length > 1)
                            return basePathFormatter.GetFormattedPath(paths);
                        var subRoot = converterTree.Traverse(primaryDependencies[0], false);
                        if(subRoot == null)
                            return basePathFormatter.GetFormattedPath(paths);
                        ModelConfigurationNode keyLeaf = subRoot.FindKeyLeaf();
                        if(keyLeaf != null)
                            performedPath = performer.Perform(keyLeaf.Path);
                        else
                        {
                            var subNodes = new List<ModelConfigurationNode>();
                            subRoot.FindSubNodes(subNodes);
                            performedPath = Expression.NewArrayInit(typeof(object), subNodes.Select(node => Expression.Convert(performer.Perform(node.Path), typeof(object))));
                        }
                    }
                    var primaryDependenciez = Expression.Lambda(performedPath, performedPath.ExtractParameters()).ExtractPrimaryDependencies();
                    if(primaryDependenciez.Length == 0)
                        return basePathFormatter.GetFormattedPath(paths);
                    migratedPaths.AddRange(performedPath.CutToChains(true, true));
                }
            }
            return resolver.Visit(pathFormatter.GetFormattedPath(migratedPaths.GroupBy(chain => new ExpressionWrapper(chain, false)).Select(grouping => grouping.Key.Expression).ToArray()));
        }

        private readonly IPathFormatter pathFormatter;
        private readonly IPathFormatter basePathFormatter;
        private readonly ModelConfigurationNode converterTree;
        private readonly CompositionPerformer performer;
        private readonly AliasesResolver resolver;
    }
}