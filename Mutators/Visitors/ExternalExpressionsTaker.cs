using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    internal class ExternalExpressionsTaker : ExpressionVisitor
    {
        public ExternalExpressionsTaker(IEnumerable<Expression> externalNodes)
        {
            this.externalNodes = new HashSet<Expression>(externalNodes);
        }

        public List<Expression> Take(Expression expression)
        {
            Visit(expression);
            return externalList;
        }

        public override Expression Visit(Expression node)
        {
            if (externalNodes.Contains(node))
            {
                externalList.Add(node);
                return node;
            }

            return base.Visit(node);
        }

        private readonly HashSet<Expression> externalNodes;
        private readonly List<Expression> externalList = new List<Expression>();
    }
}