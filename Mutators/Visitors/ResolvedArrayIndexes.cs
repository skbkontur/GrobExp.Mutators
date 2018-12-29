using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    internal class ResolvedArrayIndexes
    {
        public ParameterExpression indexes;
        public Expression indexesInit;
        public List<Expression> path;
    }
}