using System.Collections.Generic;
using System.Linq;
using System.Text;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    internal static class ModelConfigurationNodeFormatter
    {
        public static string ToPrettyString(this ModelConfigurationNode node)
        {
            var allMutators = new List<MutatorWithPath>();
            node.GetMutatorsWithPath(allMutators);
            return ToPrettyString(allMutators);
        }

        public static void GetMutatorsWithPath(this ModelConfigurationNode node, List<MutatorWithPath> result)
        {
            result.AddRange(node.GetMutatorsWithPath());
            foreach (var child in node.Children)
                GetMutatorsWithPath(child, result);
        }

        public static string ToPrettyString(this IEnumerable<MutatorWithPath> mutators)
        {
            var result = new StringBuilder();
            foreach (var group in mutators.GroupBy(pair => new ExpressionWrapper(pair.PathToNode, false)))
            {
                result.AppendLine(group.Key.Expression.ToString());
                foreach (var pair in group)
                {
                    result.Append("    PATH: ");
                    result.AppendLine(pair.PathToMutator.ToString());
                    result.Append("    MUTATOR: ");
                    result.AppendLine(pair.Mutator.ToString());
                    result.AppendLine();
                }
            }

            return result.ToString();
        }
    }
}