using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace GrobExp.Mutators.ModelConfiguration.Traverse
{
    internal class TraverseResult
    {
        [CanBeNull]
        public ModelConfigurationNode Child { get; set; }

        [NotNull]
        public List<KeyValuePair<Expression, Expression>> ArrayAliases { get; set; }

        public bool SubRootIsVisited { get; set; }
    }
}