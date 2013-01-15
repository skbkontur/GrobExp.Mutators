using System.Linq;
using System.Linq.Expressions;

namespace GrobExp
{
    public class LambdaInvocationInliner: ExpressionVisitor
    {
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            if(node.Expression.NodeType != ExpressionType.Lambda)
                return base.VisitInvocation(node);
            var lambda = (LambdaExpression)node.Expression;
            var expressions = lambda.Parameters.Select((t, i) => Expression.Assign(t, node.Arguments[i])).Cast<Expression>().ToList();
            expressions.Add(lambda.Body);
            return Expression.Block(lambda.Body.Type, lambda.Parameters, expressions);
        }
    }
}