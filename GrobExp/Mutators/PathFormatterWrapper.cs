using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class PathFormatterWrapper : IPathFormatter
    {
        public PathFormatterWrapper(IPathFormatter pathFormatter, CompositionPerformer performer, AliasesResolver resolver)
        {
            this.pathFormatter = pathFormatter;
            this.performer = performer;
            this.resolver = resolver;
        }

        public Expression GetFormattedPath(Expression[] paths)
        {
            return resolver.Visit(pathFormatter.GetFormattedPath((from path in paths
                                                                  let performedPath = performer.Perform(path)
                                                                  from dependency in Expression.Lambda(performedPath, performedPath.ExtractParameters()).ExtractPrimaryDependencies()
                                                                  from chain in dependency.Body.CutToChains(true, true)
                                                                  group chain by new ExpressionWrapper(chain, false)).Select(grouping => grouping.Key.Expression).ToArray()));
        }

        private readonly IPathFormatter pathFormatter;
        private readonly CompositionPerformer performer;
        private readonly AliasesResolver resolver;
    }
}