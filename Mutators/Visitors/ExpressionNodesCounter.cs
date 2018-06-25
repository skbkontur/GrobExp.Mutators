using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionNodesCounter : ExpressionVisitor
    {
        public int Count(Expression expression)
        {
            count = 0;
            Visit(expression);
            return count;
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
                return null;
            ++count;
            return node.NodeType == ExpressionType.Invoke ? node : base.Visit(node);
        }

        private int count;
    }
}