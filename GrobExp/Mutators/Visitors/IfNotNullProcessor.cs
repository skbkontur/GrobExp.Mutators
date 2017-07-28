using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    /// <summary>
    ///     Replaces all IfNotNull calls in binary expressions with appropriate checks
    ///     <br />
    ///     Examples:
    ///     <code>
    /// x.IfNotNull() == y.IfNotNull() ->
    /// x == null || y == null || x == y </code>
    ///     <code>
    /// Igor().IfNotNull() == "GRobas" -> // If Igor() is a 'constant' function
    /// string.IsNullOrEmpty(Igor()) || Igor() == "GRobas" </code>
    /// </summary>
    public class IfNotNullProcessor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = node.Left;
            var right = node.Right;
            var leftIsIfNotNullCall = TryUnwrapIfNotNull(ref left);
            var rightIsIfNotNullCall = TryUnwrapIfNotNull(ref right);
            if(leftIsIfNotNullCall || rightIsIfNotNullCall)
            {
                var leftIsConstant = left.IsConstant();
                var rightIsConstant = right.IsConstant();

                if(leftIsConstant)
                    left = left.ToConstant();
                if(rightIsConstant)
                    right = right.ToConstant();

                var result = Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method, node.Conversion);
                if(rightIsIfNotNullCall)
                    result = Expression.OrElse(IsEmpty(right, rightIsConstant), result);
                if(leftIsIfNotNullCall)
                    result = Expression.OrElse(IsEmpty(left, leftIsConstant), result);
                return result;
            }
            return base.VisitBinary(node);
        }

        private static Expression IsEmpty(Expression exp, bool isConstant)
        {
            if(exp.Type == typeof(string) && isConstant)
                return Expression.Call(isNullOrEmptyMethod, exp);
            return Expression.Equal(exp, Expression.Constant(null, exp.Type));
        }

        private static bool TryUnwrapIfNotNull(ref Expression node)
        {
            if(node == null || node.NodeType != ExpressionType.Call)
                return false;

            var methodCallExpression = (MethodCallExpression)node;
            if(!methodCallExpression.Method.IsGenericMethod || methodCallExpression.Method.GetGenericMethodDefinition() != ifNotNullMethod)
                return false;

            node = methodCallExpression.Arguments[0];
            return true;
        }

        private static readonly MethodInfo isNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string, bool>>)(s => string.IsNullOrEmpty(s))).Body).Method;

        private static readonly MethodInfo ifNotNullMethod = ((MethodCallExpression)((Expression<Func<int, int>>)(i => i.IfNotNull())).Body).Method.GetGenericMethodDefinition();
    }
}