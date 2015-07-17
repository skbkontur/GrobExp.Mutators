using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;

using GrEmit;

namespace GrobExp.Compiler
{
    internal class ExpressionPrivateMembersAccessor : ExpressionVisitor
    {
        private static bool IsNestedlyPublic(Type type)
        {
            if (type == null) return true;

            if(type.IsArray)
            {
                if (!type.IsNested)
                {
                    if (!type.IsPublic)
                        return false;
                }
                else if (!type.IsNestedPublic || !IsNestedlyPublic(type.DeclaringType))
                    return false;
                var elem = type.GetElementType();
                return IsNestedlyPublic(elem);
            }

            if(type.IsGenericType)
            {
                if(!type.IsNested)
                {
                    if(!type.IsPublic)
                        return false;
                }
                else if(!type.IsNestedPublic || !IsNestedlyPublic(type.DeclaringType))
                    return false;
                var parameters = type.GetGenericArguments();
                return parameters.All(IsNestedlyPublic);
            }

            if (!type.IsNested)
                return type.IsPublic;
            return type.IsNestedPublic && IsNestedlyPublic(type.DeclaringType);
        }

        private static Expression GetGetter(FieldInfo field, Expression obj, Type type)
        {
            var extractor = FieldsExtractor.GetExtractor(field);
            return Expression.Convert(Expression.Invoke(Expression.Constant(extractor), obj ?? Expression.Constant(null)), type);
        }

        private static Expression GetSetter(FieldInfo field, Expression obj, Expression newValue)
        {
            var setter = FieldsExtractor.GetSetter(field);
            return Expression.Invoke(Expression.Constant(setter), obj ?? Expression.Constant(null), Expression.Convert(newValue, typeof(object)));
        }

        private static bool NeedsToBeReplacedByGetter(Expression node)
        {
            var access = node as MemberExpression;
            if (access == null)
                return false;
            return NeedsToBeReplacedByGetter(access);
        }

        private static bool NeedsToBeReplacedByGetter(MemberExpression node)
        {
            var member = node.Member;
            var expression = node.Expression;

            return member.MemberType == MemberTypes.Field &&
                   ((expression != null && !IsNestedlyPublic(expression.Type)) ||
                    !((FieldInfo)member).Attributes.HasFlag(FieldAttributes.Public));
        }

        private static Expression GetObjectFromGetter(Expression getter)
        {
            var convert = (UnaryExpression)getter;
            var invocation = (InvocationExpression)convert.Operand;
            var obj = invocation.Arguments[0];
            var asConst = obj as ConstantExpression;
            if(asConst != null && asConst.Value == null)
                return null;
            return obj;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var member = node.Member;
            var expression = Visit(node.Expression);

            if (NeedsToBeReplacedByGetter(node))
            {
                if(expression != null && expression.NodeType == ExpressionType.Convert)
                    expression = ((UnaryExpression)expression).Operand;
                return GetGetter((FieldInfo)member, expression, node.Type);
            }

            return node.Update(expression);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return base.VisitLambda(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var newObject = Visit(node.Object);
            var arguments = node.Arguments;
            var newArguments = Visit(arguments).ToArray();
            var argumentsTypes = node.Method.GetParameters().Select(p => p.ParameterType).ToArray();

            if(node.Method.IsPrivate)
            {
                if(argumentsTypes.Any(t => !IsNestedlyPublic(t)) || !IsNestedlyPublic(node.Method.ReturnType))
                    throw new InvalidOperationException(
                        string.Format("Private method '{0}' with arguments or return value of private types is not allowed!",
                            node.Method.Name));
            }

            var variables = new List<ParameterExpression>();
            var beforeInvocation = new List<Expression>();
            var afterInvocation = new List<Expression>();

            for(int i = 0; i < arguments.Count; i++)
            {
                if(NeedsToBeReplacedByGetter(arguments[i]) && argumentsTypes[i].IsByRef)
                {
                    var access = (MemberExpression)arguments[i];
                    var getter = newArguments[i];
                    var local = Expression.Parameter(argumentsTypes[i].GetElementType());
                    var setter = GetSetter((FieldInfo)access.Member, GetObjectFromGetter(getter), local);

                    variables.Add(local);
                    beforeInvocation.Add(Expression.Assign(local, getter));
                    afterInvocation.Add(setter);
                    newArguments[i] = local;
                }
            }

            Expression newInvocation;
            if(node.Method.IsPublic && IsNestedlyPublic(node.Method.DeclaringType))
                newInvocation = node.Update(newObject, newArguments);
            else
            {
                if(newObject != null)
                    newArguments = new[] {newObject}.Concat(newArguments).ToArray();

                var methodDelegate = MethodInvokerBuilder.GetInvoker(node.Method);
                var methodDelegateType = Extensions.GetDelegateType(newArguments.Select(a => a.Type).ToArray(), node.Method.ReturnType);

                newInvocation = Expression.Invoke(Expression.Convert(Expression.Constant(methodDelegate), methodDelegateType), newArguments);
            }

            if(variables.Count > 0)
            {
                ParameterExpression returnVariable = null;
                if(newInvocation.Type != typeof(void))
                {
                    returnVariable = Expression.Parameter(newInvocation.Type);
                    variables.Add(returnVariable);
                    newInvocation = Expression.Assign(returnVariable, newInvocation);
                }

                var blockExpressions = new List<Expression>();
                blockExpressions.AddRange(beforeInvocation);
                blockExpressions.Add(newInvocation);
                blockExpressions.AddRange(afterInvocation);
                if(returnVariable != null)
                    blockExpressions.Add(returnVariable);

                return Expression.Block(variables, blockExpressions);
            }

            return newInvocation;
        }
    }
}