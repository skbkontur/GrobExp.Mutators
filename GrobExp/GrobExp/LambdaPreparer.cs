using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using GrobExp.ExpressionEmitters;

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

        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            var constructor = typeof(RuntimeVariables).GetConstructor(new[] {typeof(object[])});
            return Expression.New(constructor, Expression.NewArrayInit(typeof(object), node.Variables.Select(parameter => parameter.Type.IsValueType ? Expression.Convert(parameter, typeof(object)) : (Expression)parameter)));
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if(node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
            {
                var left = Visit(node.Left);
                var right = Visit(node.Right);
                if(left.Type.IsNullable() && right.Type == typeof(object))
                    right = Expression.Convert(right, node.Left.Type);
                return node.Update(left, (LambdaExpression)Visit(node.Conversion), right);
            }
            return base.VisitBinary(node);
        }
    }
}