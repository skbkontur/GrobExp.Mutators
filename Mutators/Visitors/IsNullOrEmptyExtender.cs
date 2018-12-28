using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    internal class IsNullOrEmptyExtender : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
            {
                var leftIsNull = node.Left.IsNull();
                var rightIsNull = node.Right.IsNull();
                if (leftIsNull || rightIsNull)
                {
                    if (leftIsNull && rightIsNull)
                        return node;
                    var exp = leftIsNull ? node.Right : node.Left;
                    if (TryGetIsNullOrEmptyMethod(exp.Type, out var isNullOrEmptyMethod))
                    {
                        Expression result = Expression.Call(null, isNullOrEmptyMethod, new[] {exp});
                        if (node.NodeType == ExpressionType.NotEqual)
                            result = Expression.Not(result);
                        return result;
                    }

                    if (!IsStandardType(exp.Type))
                    {
                        var properties = ExtractProperties(exp);
                        Expression result = null;
                        foreach (var property in properties)
                        {
                            Expression current = CanBeNull(property.Type) ? Visit(Expression.Equal(property, Expression.Constant(null, property.Type))) : Expression.Equal(property, Expression.Default(property.Type));
                            result = result == null ? current : Expression.AndAlso(result, current);
                        }

                        if (result == null)
                            result = CanBeNull(exp.Type) ? Expression.Equal(exp, Expression.Constant(null, exp.Type)) : Expression.Equal(exp, Expression.Default(exp.Type));
                        if (node.NodeType == ExpressionType.NotEqual)
                            result = Expression.Not(result);
                        return result;
                    }
                }
            }

            return base.VisitBinary(node);
        }

        private static IEnumerable<Expression> ExtractProperties(Expression node)
        {
            if (IsStandardType(node.Type))
            {
                yield return node;
                yield break;
            }

            var properties = node.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var exp in properties.SelectMany(property => ExtractProperties(Expression.MakeMemberAccess(node, property))))
                yield return exp;
        }

        private static bool CanBeNull(Type type)
        {
            return !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static bool IsStandardType(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type.IsArray
                   || type == typeof(DateTime) || type == typeof(decimal) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static bool TryGetIsNullOrEmptyMethod(Type type, out MethodInfo method)
        {
            if (isNullOrEmptyMethods.TryGetValue(type, out method))
                return true;
            if (type == typeof(IEnumerable<string>) || type.GetInterfaces().Any(interfaCe => interfaCe == typeof(IEnumerable<string>)))
            {
                method = MutatorsHelperFunctions.StringArrayIsNullOrEmptyMethod;
                return true;
            }

            if (type.IsArray || type == typeof(IEnumerable) || type.GetInterfaces().Any(interfaCe => interfaCe == typeof(IEnumerable)))
            {
                method = MutatorsHelperFunctions.ArrayIsNullOrEmptyMethod;
                return true;
            }

            return false;
        }

        private static readonly MethodInfo stringIsNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string, bool>>)(s => string.IsNullOrEmpty(s))).Body).Method;

        private static readonly Dictionary<Type, MethodInfo> isNullOrEmptyMethods = new Dictionary<Type, MethodInfo>
            {
                {typeof(string), stringIsNullOrEmptyMethod},
                {typeof(string[]), MutatorsHelperFunctions.StringArrayIsNullOrEmptyMethod}
            };
    }
}