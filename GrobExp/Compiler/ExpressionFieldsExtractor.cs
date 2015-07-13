using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.Compiler
{
    class ExpressionFieldsExtractor : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            var member = node.Member;

            var expression = Visit(node.Expression);

            if (expression != null && member.MemberType == MemberTypes.Field &&
                (expression.Type.IsNestedPrivate || !((FieldInfo)member).Attributes.HasFlag(FieldAttributes.Public)))
            {
                var extractor = FieldsExtractor.GetExtractor(member as FieldInfo);
                if(expression.NodeType == ExpressionType.Convert)
                    expression = ((UnaryExpression)expression).Operand;
                return Expression.Convert(Expression.Invoke(Expression.Constant(extractor), expression), node.Type);
            }

            return node.Update(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if(node.Method.Attributes.HasFlag(MethodAttributes.Public))
                return base.VisitMethodCall(node);

            var arguments = new List<Expression>();
            if (node.Object != null)
                arguments.Add(Visit(node.Object));
            arguments.AddRange(Visit(node.Arguments));

            var methodDelegate = CreateDynamicMethod(node.Method);
            var methodDelegateType = Extensions.GetDelegateType(arguments.Select(e => e.Type).ToArray(), node.Method.ReturnType);

            return Expression.Call(Expression.Convert(Expression.Constant(methodDelegate), methodDelegateType),
                methodDelegateType.GetMethod("Invoke"), arguments);
        }

        private static Delegate CreateDynamicMethod(MethodInfo method)
        {
            throw new NotImplementedException();
        }
    }
}