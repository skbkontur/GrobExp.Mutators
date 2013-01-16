using System.Linq.Expressions;

namespace GrobExp
{
    public class ExtensionReducer: ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node)
        {
            return node.CanReduce ? Visit(node.Reduce()) : base.VisitExtension(node);
        }
    }
}