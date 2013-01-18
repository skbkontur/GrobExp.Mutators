using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace GrobExp
{
    internal class LambdaPreparer : ExpressionVisitor
    {
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            if(node.Expression.NodeType != ExpressionType.Lambda)
                return base.VisitInvocation(node);
            var lambda = (LambdaExpression)node.Expression;
            var expressions = lambda.Parameters.Select((t, i) => Expression.Assign(t, Visit(node.Arguments[i]))).Cast<Expression>().ToList();
            expressions.Add(Visit(lambda.Body));
            return Expression.Block(lambda.Body.Type, lambda.Parameters, expressions);
        }

        protected override Expression VisitExtension(Expression node)
        {
            return node.CanReduce ? Visit(node.Reduce()) : base.VisitExtension(node);
        }

        protected override Expression VisitDynamic(DynamicExpression node)
        {
            CallSite site = CallSite.Create(node.DelegateType, node.Binder);
            Type siteType = site.GetType();
            ConstantExpression constant = Expression.Constant(site, siteType);
            return Expression.Call(Expression.MakeMemberAccess(constant, siteType.GetField("Target")), node.DelegateType.GetMethod("Invoke"), new[] {constant}.Concat(node.Arguments.Select(Visit)));
        }
    }
}