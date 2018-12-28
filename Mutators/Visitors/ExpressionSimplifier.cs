using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    // todo ich: ускорить этого медленного монстра, сейчас он работает за квадрат от размера Expression-а
    /// <summary>
    ///     Работает за квадрат из-за того, что при обходе Expression-а внутри Visit вызывается IsConstant который точно также обходит всё поддерево.
    /// </summary>
    internal class ExpressionSimplifier : ExpressionVisitor
    {
        public Expression Simplify(Expression expression)
        {
            expression = new IfNotNullProcessor().Visit(expression);
            return Visit(expression);
        }

        /// <summary>
        ///     Игнорируем Expression-ы, помеченные Dynamic().
        ///     Expression-ы, которые считаются "константными" в особом мутаторном смысле, компилируются в функции и заменяются на Expression.Constant.
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null;
            if (exp.NodeType == ExpressionType.Call && ((MethodCallExpression)exp).Method.IsDynamicMethod())
                return exp;
            if (exp.NodeType == ExpressionType.Lambda || exp.NodeType == ExpressionType.Goto ||
                exp.NodeType == ExpressionType.Label || exp.NodeType == ExpressionType.Default ||
                exp.NodeType == ExpressionType.New || exp.NodeType == ExpressionType.MemberInit ||
                exp.NodeType == ExpressionType.Constant || !exp.IsConstant())
                return base.Visit(exp);

            if (exp.IsOfType(ExpressionType.Convert))
            {
                var unaryExpression = (UnaryExpression)exp;
                exp = TryRemoveEnumToIntCast(unaryExpression, unaryExpression.Operand) ?? unaryExpression;
            }

            return VisitConstant(exp.ToConstant());
        }

        /// <summary>
        ///     Не посещает параметры метода, переданные по ref и по out
        /// </summary>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var parameters = node.Method.GetParameters();
            if (parameters.All(param => !param.ParameterType.IsByRef))
                return base.VisitMethodCall(node);
            return Expression.Call(Visit(node.Object), node.Method, node.Arguments.Select((x, i) => parameters[i].ParameterType.IsByRef ? x : Visit(x)));
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression.IsAnonymousTypeCreation())
                return InlineAnonymousTypeField(node);

            return TryUnwrapConversion(node) ?? base.VisitMember(node);
        }

        /// <summary>
        ///     Пробуем удалить каст
        /// </summary>
        private Expression TryUnwrapConversion(MemberExpression node)
        {
            if (!node.Expression.IsOfType(ExpressionType.Convert))
                return null;

            var unaryExpression = (UnaryExpression)node.Expression;
            var member = unaryExpression.Operand.Type
                                        .GetMember(node.Member.Name)
                                        .FirstOrDefault(memberInfo => HasType(memberInfo, node.Type));
            if (member == null)
                return null;

            return Expression.MakeMemberAccess(Visit(unaryExpression.Operand), member);
        }

        /// <summary>
        ///     Инлайним обращение к полю анонимного типа:
        ///     <code>
        /// z => new {X = z.Y}.X ->
        /// z => z.Y </code>
        /// </summary>
        private Expression InlineAnonymousTypeField(MemberExpression node)
        {
            var newExpression = (NewExpression)Visit(node.Expression);
            var type = newExpression.Type;
            MemberInfo[] members;

            if (node.Member is FieldInfo)
                // ReSharper disable once CoVariantArrayConversion
                members = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            else if (node.Member is PropertyInfo)
                // ReSharper disable once CoVariantArrayConversion
                members = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            else throw new NotSupportedException();

            var i = Array.IndexOf(members, node.Member);
            if (i < 0 || i >= newExpression.Arguments.Count)
                throw new InvalidOperationException();

            return newExpression.Arguments[i];
        }

        /// <summary>
        ///     Упрощаем логические выражения в случае, если известно значение одного из операндов.
        ///     Подхачиваем конвертации енумов в сравнениях.
        /// </summary>
        protected override Expression VisitBinary(BinaryExpression b)
        {
            switch (b.NodeType)
            {
            case ExpressionType.AndAlso:
                {
                    var left = Visit(b.Left);
                    var right = Visit(b.Right);
                    if (left is ConstantExpression && (left.Type == typeof(bool)))
                    {
                        if ((bool)(left as ConstantExpression).Value)
                            return right;
                        return left;
                    }

                    if (right is ConstantExpression && (right.Type == typeof(bool)))
                    {
                        if ((bool)(right as ConstantExpression).Value)
                            return left;
                        return right;
                    }

                    return b.Update(left, VisitAndConvert(b.Conversion, "VisitBinary"), right);
                }
            case ExpressionType.OrElse:
                {
                    var left = Visit(b.Left);
                    var right = Visit(b.Right);
                    if (left is ConstantExpression && (left.Type == typeof(bool)))
                    {
                        if ((bool)(left as ConstantExpression).Value)
                            return left;
                        return right;
                    }

                    if (right is ConstantExpression && (right.Type == typeof(bool)))
                    {
                        if ((bool)(right as ConstantExpression).Value)
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
                    if (leftIsAConvertFromEnum || rightIsAConvertFromEnum)
                    {
                        if (!leftIsAConvertFromEnum)
                            left = Expression.Convert(left, ((UnaryExpression)right).Operand.Type);
                        if (!rightIsAConvertFromEnum)
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
            if (u.IsOfType(ExpressionType.Convert))
            {
                var operand = Visit(u.Operand);
                return TryRemoveEnumToIntCast(u, operand) ?? u.Update(operand);
            }

            return base.VisitUnary(u);
        }

        /// <summary>
        ///     Убирает касты енума (nullable енума) к инту (nullable инту)
        /// </summary>
        private static Expression TryRemoveEnumToIntCast(UnaryExpression unaryExpression, Expression operand)
        {
            if (operand.Type.IsEnum)
            {
                if (unaryExpression.Type == typeof(int))
                    return operand;
                if (unaryExpression.Type == typeof(int?))
                    return Expression.Convert(operand, typeof(Nullable<>).MakeGenericType(operand.Type));
            }
            else if (IsNullableEnum(operand.Type))
            {
                if (unaryExpression.Type == typeof(int))
                    return Expression.Convert(operand, operand.Type.GetGenericArguments()[0]);
                if (unaryExpression.Type == typeof(int?))
                    return operand;
            }

            return null;
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            var test = Visit(c.Test);
            if (test.NodeType == ExpressionType.Constant)
                return (bool)((ConstantExpression)test).Value ? Visit(c.IfTrue) : Visit(c.IfFalse);
            return c.Update(test, Visit(c.IfTrue), Visit(c.IfFalse));
        }

        private static bool HasType(MemberInfo member, Type type)
        {
            return (member is PropertyInfo && ((PropertyInfo)member).PropertyType == type)
                   || (member is FieldInfo && ((FieldInfo)member).FieldType == type);
        }

        private static bool IsNullableEnum(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GetGenericArguments()[0].IsEnum;
        }

        private static bool IsAConvertFromEnumToInt(Expression exp)
        {
            return exp.IsOfType(ExpressionType.Convert) &&
                   ((exp.Type == typeof(int) && ((UnaryExpression)exp).Operand.Type.IsEnum) ||
                    (exp.Type == typeof(int?) && IsNullableEnum(((UnaryExpression)exp).Operand.Type)));
        }
    }
}