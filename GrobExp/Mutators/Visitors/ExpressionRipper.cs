using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionRipper : ExpressionVisitor
    {
        public Expression[] Cut(Expression expression, bool rootOnlyParameter, bool hard)
        {
            this.rootOnlyParameter = rootOnlyParameter;
            this.hard = hard;
            chains = new List<Expression>();
            Visit(expression);
            return chains.ToArray();
        }

        public override Expression Visit(Expression node)
        {
            if(!node.IsLinkOfChain(rootOnlyParameter, hard) || node.IsStringLengthPropertyAccess())
                return base.Visit(node);
            chains.Add(node);
            return node;
        }

        private List<Expression> chains;
        private bool hard;
        private bool rootOnlyParameter;
    }
}