using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ChainsExtractor : ExpressionVisitor
    {

        public Expression[] Extract(Expression expression)
        {
            index = 0;
            chains = new Dictionary<Expression, int>();
            localParameters = new HashSet<ParameterExpression>();
            Visit(expression);
            return chains.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();
        }

        public override Expression Visit(Expression node)
        {
            if (node.IsLinkOfChain(true, true) && !localParameters.Contains(node.SmashToSmithereens()[0]))
            {
                if(!chains.ContainsKey(node))
                    chains[node] = index++;
                return node;
            }
            return base.Visit(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach(var variable in node.Variables)
                localParameters.Add(variable);
            var result = base.VisitBlock(node);
            foreach(var variable in node.Variables)
                localParameters.Remove(variable);
            return result;
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            foreach (var parameter in lambda.Parameters)
                localParameters.Add(parameter);
            var result = base.VisitLambda(lambda);
            foreach (var parameter in lambda.Parameters)
                localParameters.Remove(parameter);
            return result;
        }

        private HashSet<ParameterExpression> localParameters;
        private Dictionary<Expression, int> chains;
        private int index;
    }
}
