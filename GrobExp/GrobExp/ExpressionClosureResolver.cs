using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp
{
    public class ExpressionClosureResolver : ExpressionVisitor
    {
        public ExpressionClosureResolver(LambdaExpression lambda)
        {
            this.lambda = lambda;
            closureType = new ExpressionClosureBuilder(lambda).Build(out constants, out parameters);
            closureParameter = Expression.Parameter(closureType);
        }

        public LambdaExpression Resolve(out ParameterExpression closureParameter)
        {
            var body = ((LambdaExpression)Visit(lambda)).Body;
            var delegatesParameter = Expression.Parameter(typeof(Delegate[]));
            Expression createClosure = Expression.Assign(this.closureParameter, Expression.New(closureType));
            Expression initClosure = Expression.Assign(Expression.MakeMemberAccess(this.closureParameter, Closure.DelegatesField), delegatesParameter);
            closureParameter = this.closureParameter;
            return Expression.Lambda(Expression.Block(body.Type, new[] {this.closureParameter}, createClosure, initClosure, body), new[] {delegatesParameter}.Concat(lambda.Parameters));
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            localParameters.Push(new HashSet<ParameterExpression>(node.Parameters));
            var body = base.Visit(node.Body);
            localParameters.Pop();
            var assigns = new List<Expression>();
            foreach(var parameter in node.Parameters)
            {
                FieldInfo field;
                if(parameters.TryGetValue(parameter, out field))
                    assigns.Add(Expression.Assign(Expression.MakeMemberAccess(closureParameter, field), parameter.Type == field.FieldType ? (Expression)parameter : Expression.New(field.FieldType.GetConstructor(new[] {parameter.Type}), parameter)));
            }
            return Expression.Lambda<T>(assigns.Count == 0 ? body : Expression.Block(body.Type, assigns.Concat(new[] {body})), node.Parameters);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            var peek = localParameters.Peek();
            var variables = node.Variables.Where(variable => !parameters.ContainsKey(variable)).ToArray();
            foreach(var variable in variables)
                peek.Add(variable);
            var expressions = node.Expressions.Select(Visit);
            foreach(var variable in variables)
                peek.Remove(variable);
            return node.Update(variables, expressions);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            FieldInfo field;
            return constants.TryGetValue(node, out field)
                       ? (field.FieldType == node.Type
                              ? Expression.MakeMemberAccess(null, field)
                              : Expression.MakeMemberAccess(Expression.MakeMemberAccess(null, field), field.FieldType.GetField("Value", BindingFlags.Public | BindingFlags.Instance)))
                       : base.VisitConstant(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            FieldInfo field;
            return (!localParameters.Peek().Contains(node) || node.Type.IsValueType) && parameters.TryGetValue(node, out field)
                       ? node.Type == field.FieldType
                             ? Expression.MakeMemberAccess(closureParameter, field)
                             : Expression.MakeMemberAccess(Expression.MakeMemberAccess(closureParameter, field), field.FieldType.GetField("Value", BindingFlags.Public | BindingFlags.Instance))
                       : base.VisitParameter(node);
        }

        private readonly Stack<HashSet<ParameterExpression>> localParameters = new Stack<HashSet<ParameterExpression>>();

        private readonly LambdaExpression lambda;
        private readonly ParameterExpression closureParameter;
        private readonly Type closureType;
        private readonly Dictionary<ConstantExpression, FieldInfo> constants;
        private readonly Dictionary<ParameterExpression, FieldInfo> parameters;
    }
}