using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public class IsNullOrEmptyExtender : ExpressionVisitor
    {
        public static readonly MethodInfo StringIsNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string, bool>>)(s => string.IsNullOrEmpty(s))).Body).Method;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if(node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
            {
                var leftIsNull = node.Left.IsNull();
                var rightIsNull = node.Right.IsNull();
                if(leftIsNull || rightIsNull)
                {
                    if(leftIsNull && rightIsNull)
                        return node;
                    var exp = leftIsNull ? node.Right : node.Left;
                    MethodInfo isNullOrEmptyMethod;
                    if(TryGetIsNullOrEmptyMethod(exp.Type, out isNullOrEmptyMethod))
                    {
                        Expression result = Expression.Call(null, isNullOrEmptyMethod, new[] {exp});
                        if(node.NodeType == ExpressionType.NotEqual)
                            result = Expression.Not(result);
                        return result;
                    }
                    if(!IsStandardType(exp.Type))
                    {
                        var properties = ExtractProperties(exp);
                        Expression result = null;
                        foreach(var property in properties)
                        {
                            Expression current = CanBeNull(property.Type) ? Visit(Expression.Equal(property, Expression.Constant(null, property.Type))) : Expression.Equal(property, Expression.Default(property.Type));
                            result = result == null ? current : Expression.AndAlso(result, current);
                        }
                        if(result == null) result = Expression.Constant(false);
                        if(node.NodeType == ExpressionType.NotEqual)
                            result = Expression.Not(result);
                        return result;
                    }
                }
            }
            return base.VisitBinary(node);
        }

        private static IEnumerable<Expression> ExtractProperties(Expression node)
        {
            if(IsStandardType(node.Type))
            {
                yield return node;
                yield break;
            }
            var properties = node.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach(var exp in properties.SelectMany(property => ExtractProperties(Expression.MakeMemberAccess(node, property))))
                yield return exp;
        }

        private static bool CanBeNull(Type type)
        {
            return !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static bool IsStandardType(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || (type.IsArray && IsStandardType(type.GetElementType()))
                   || type == typeof(DateTime) || type == typeof(decimal) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static bool TryGetIsNullOrEmptyMethod(Type type, out MethodInfo method)
        {
            if(isNullOrEmptyMethods.TryGetValue(type, out method))
                return true;
            if(type.IsArray)
            {
                method = MutatorsHelperFunctions.ArrayIsNullOrEmptyMethod;
                return true;
            }
            return false;
        }

        private static readonly Dictionary<Type, MethodInfo> isNullOrEmptyMethods = new Dictionary<Type, MethodInfo>
            {
                {typeof(string), StringIsNullOrEmptyMethod},
                {typeof(string[]), MutatorsHelperFunctions.StringArrayIsNullOrEmptyMethod}
            };
    }
}