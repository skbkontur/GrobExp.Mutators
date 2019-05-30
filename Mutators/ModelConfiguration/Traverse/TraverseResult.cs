using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.ModelConfiguration.Traverse
{
    internal class TraverseResult
    {
        public ModelConfigurationNode Child { get; set; }

        public List<KeyValuePair<Expression, Expression>> ArrayAliases { get; set; }

        public bool SubRootIsVisited { get; set; }
    }
}