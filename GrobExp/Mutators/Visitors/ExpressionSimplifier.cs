using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    // todo ich: ускорить этого медленного монстра, сейчас он работает за квадрат от размера Expression-а
    public class ExpressionSimplifier : ExpressionVisitor
    {
        public Expression Simplify(Expression expression)
        {
            expression = new MutatorsHelperFunctionsProcessor().Visit(expression);
            return Visit(expression);
        }

        public override Expression Visit(Expression exp)
        {
            if(exp == null)
                return null;
            if(exp.NodeType == ExpressionType.Call && ((MethodCallExpression)exp).Method.IsDynamicMethod())
                return exp;
            if(exp.NodeType != ExpressionType.Lambda && exp.NodeType != ExpressionType.Goto
                && exp.NodeType != ExpressionType.Label && exp.NodeType != ExpressionType.Default
                && exp.NodeType != ExpressionType.New && exp.NodeType != ExpressionType.MemberInit
                && exp.NodeType != ExpressionType.Constant && IsConstant(exp)) // todo ich: очень странная вещь.. надо как-то получше сделать
            {
                if(exp.NodeType == ExpressionType.Convert)
                {
                    var unaryExpression = (UnaryExpression)exp;
                    if(unaryExpression.Operand.Type.IsEnum)
                    {
                        if(unaryExpression.Type == typeof(int))
                            exp = unaryExpression.Operand;
                        else if(unaryExpression.Type == typeof(int?))
                            exp = Expression.Convert(unaryExpression.Operand, typeof(Nullable<>).MakeGenericType(unaryExpression.Operand.Type));
                    }
                    else if(IsNullableEnum(unaryExpression.Operand.Type))
                    {
                        if(unaryExpression.Type == typeof(int?))
                            exp = unaryExpression.Operand;
                    }
                }
                return VisitConstant(exp.ToConstant());
            }
            return base.Visit(exp);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var parameters = node.Method.GetParameters();
            if(!parameters.Any(param => param.ParameterType.IsByRef))
                return base.VisitMethodCall(node);
            return Expression.Call(Visit(node.Object), node.Method, node.Arguments.Select((x, i) => parameters[i].ParameterType.IsByRef ? x : Visit(x)));
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if(!node.Expression.IsAnonymousTypeCreation())
            {
                if(node.Expression != null && node.Expression.NodeType == ExpressionType.Convert)
                {
                    var unaryExpression = (UnaryExpression)node.Expression;
                    var member = unaryExpression.Operand.Type.GetMember(node.Member.Name).FirstOrDefault(memberInfo => HasType(memberInfo, node.Type));
                    if(member != null)
                        return Expression.MakeMemberAccess(Visit(unaryExpression.Operand), member);
                }
                return base.VisitMember(node);
            }
            var newExpression = (NewExpression)Visit(node.Expression);
            var type = newExpression.Type;
            MemberInfo[] members;
            if(node.Member is FieldInfo)
                members = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            else if(node.Member is PropertyInfo)
                members = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            else throw new NotSupportedException();
            var i = Array.IndexOf(members, node.Member);
            if(i < 0 || i >= newExpression.Arguments.Count) throw new InvalidOperationException();
            return newExpression.Arguments[i];
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            switch(b.NodeType)
            {
            case ExpressionType.AndAlso:
                {
                    var left = Visit(b.Left);
                    var right = Visit(b.Right);
                    if((left is ConstantExpression) && (left.Type == typeof(bool)))
                    {
                        if((bool)((left as ConstantExpression).Value))
                            return right;
                        return left;
                    }
                    if((right is ConstantExpression) && (right.Type == typeof(bool)))
                    {
                        if((bool)((right as ConstantExpression).Value))
                            return left;
                        return right;
                    }
                    return b.Update(left, VisitAndConvert(b.Conversion, "VisitBinary"), right);
                }
            case ExpressionType.OrElse:
                {
                    var left = Visit(b.Left);
                    var right = Visit(b.Right);
                    if((left is ConstantExpression) && (left.Type == typeof(bool)))
                    {
                        if((bool)((left as ConstantExpression).Value))
                            return left;
                        return right;
                    }
                    if((right is ConstantExpression) && (right.Type == typeof(bool)))
                    {
                        if((bool)((right as ConstantExpression).Value))
                            return right;
                        return left;
                    }
                    return b.Update(left, VisitAndConvert(b.Conversion, "VisitBinary"), right);
                }
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                {
                    var left = b.Left;
                    var right = b.Right;
                    var leftIsAConvertFromEnum = IsAConvertFromEnumToInt(left);
                    var rightIsAConvertFromEnum = IsAConvertFromEnumToInt(right);
                    if(leftIsAConvertFromEnum || rightIsAConvertFromEnum)
                    {
                        if(!leftIsAConvertFromEnum)
                            left = Expression.Convert(left, ((UnaryExpression)right).Operand.Type);
                        if(!rightIsAConvertFromEnum)
                            right = Expression.Convert(right, ((UnaryExpression)left).Operand.Type);
                        return b.Update(Visit(left), VisitAndConvert(b.Conversion, "VisitBinary"), Visit(right));
                    }
                    return base.VisitBinary(b);
                }
            }
            return base.VisitBinary(b);
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if(u.NodeType == ExpressionType.Convert)
            {
                var operand = Visit(u.Operand);
                if(operand.Type.IsEnum)
                {
                    if(u.Type == typeof(int))
                        return operand;
                    if(u.Type == typeof(int?))
                        return Expression.Convert(operand, typeof(Nullable<>).MakeGenericType(operand.Type));
                }
                else if(IsNullableEnum(operand.Type))
                {
                    var type = operand.Type.GetGenericArguments()[0];
                    if(u.Type == typeof(int))
                        return Expression.Convert(operand, type);
                    if(u.Type == typeof(int?))
                        return operand;
                }
                return u.Update(operand);
            }
            return base.VisitUnary(u);
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            var test = Visit(c.Test);
            if(test.NodeType == ExpressionType.Constant)
                return (bool)((ConstantExpression)test).Value ? Visit(c.IfTrue) : Visit(c.IfFalse);
            return base.VisitConditional(c);
        }

        private static bool HasType(MemberInfo member, Type type)
        {
            return (member is PropertyInfo && ((PropertyInfo)member).PropertyType == type)
                   || (member is FieldInfo && ((FieldInfo)member).FieldType == type);
        }

        private static bool IsConstant(Expression exp)
        {
            return new IsConstantChecker().IsConstant(exp);
        }

        private static bool IsNullableEnum(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GetGenericArguments()[0].IsEnum;
        }

        private static bool IsAConvertFromEnumToInt(Expression exp)
        {
            return exp.NodeType == ExpressionType.Convert && ((exp.Type == typeof(int) && ((UnaryExpression)exp).Operand.Type.IsEnum) || (exp.Type == typeof(int?) && IsNullableEnum(((UnaryExpression)exp).Operand.Type)));
        }
    }
}