using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators
{
    public static class ExpressionBuilder
    {
        public static Expression MakeEachCall(this Expression expression)
        {
            return Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(expression.Type.GetItemType()), expression);
        }

        public static Expression MakeEachCall(this Expression expression, Type itemType)
        {
            return Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(itemType), expression);
        }

        public static Expression MakeCurrentIndexCall(this Expression expression, Type itemType)
        {
            return Expression.Call(null, MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(itemType), expression);
        }

        public static Expression MakeConvertation(this Expression expression, Type type)
        {
            return Expression.Convert(expression, type);
        }

        public static Expression MakeArrayIndex(this Expression expression, int index)
        {
            return Expression.ArrayIndex(expression, Expression.Constant(index));
        }

        public static Expression MakeMemberAccess(this Expression expression, MemberInfo memberInfo)
        {
            return Expression.MakeMemberAccess(expression, memberInfo);
        }

        public static MethodCallExpression MakeIndexerCall(this Expression fullPath, object[] indexes, Type type)
        {
            var method = type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
            var parameters = method.GetParameters();
            return Expression.Call(fullPath, method, indexes.Select((o, i) => Expression.Constant(o, parameters[i].ParameterType)));
        }
    }
}