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
            if(!node.IsLinkOfChain(rootOnlyParameter, hard) || node.IsStringLengthPropertyAccess() || localParameters.Contains((ParameterExpression)node.SmashToSmithereens()[0]))
                return base.Visit(node);
            chains.Add(node);
            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach (var variable in node.Variables)
                localParameters.Add(variable);
            var res = base.VisitBlock(node);
            foreach (var variable in node.Variables)
                localParameters.Remove(variable);
            return res;
        }

        private readonly HashSet<ParameterExpression> localParameters = new HashSet<ParameterExpression>();
        private List<Expression> chains;
        private bool hard;
        private bool rootOnlyParameter;
    }
}