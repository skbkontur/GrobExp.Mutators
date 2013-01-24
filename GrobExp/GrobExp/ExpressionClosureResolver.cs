using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp
{
    internal class ExpressionClosureResolver : ExpressionVisitor
    {
        public ExpressionClosureResolver(LambdaExpression lambda)
        {
            lambda = (LambdaExpression)new LambdaPreparer().Visit(lambda);
            bool hasSubLambdas;
            this.lambda = new ExpressionClosureBuilder(lambda).Build(out closureType, out closureParameter, out constants, out parameters, out switches, out hasSubLambdas);
            closureParameter = parameters.Count > 0 || hasSubLambdas ? Expression.Parameter(closureType) : null;
        }

        public LambdaExpression Resolve(out Type closureType, out ParameterExpression closureParameter, out Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches)
        {
            var body = ((LambdaExpression)Visit(lambda)).Body;
            closureParameter = this.closureParameter;
            closureType = this.closureType;
            switches = this.switches;
            if(closureParameter != null)
                body = Expression.Block(new[] {closureParameter}, Expression.Assign(closureParameter, Expression.New(closureType)), body);
            var delegateType = Extensions.GetDelegateType(lambda.Parameters.Select(parameter => parameter.Type).ToArray(), lambda.ReturnType);
            return Expression.Lambda(delegateType, body, lambda.Name, lambda.TailCall, lambda.Parameters);
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
            return Expression.Lambda<T>(assigns.Count == 0 ? body : Expression.Block(body.Type, assigns.Concat(new[] {body})), node.Name, node.TailCall, node.Parameters);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            var peek = localParameters.Peek();
            var variables = node.Variables.Where(variable => !parameters.ContainsKey(variable) && !peek.Contains(variable)).ToArray();
            foreach(var variable in variables)
                peek.Add(variable);
            var expressions = node.Expressions.Select(Visit);
            foreach(var variable in variables)
                peek.Remove(variable);
            return node.Update(variables, expressions);
        }

        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            var peek = localParameters.Peek();
            var variable = node.Variable;
            if(variable != null && (peek.Contains(variable) || parameters.ContainsKey(variable)))
                variable = null;
            if(variable != null)
                peek.Add(variable);
            var res = base.VisitCatchBlock(node);
            if(variable != null)
                peek.Remove(variable);
            return res;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            FieldInfo field;
            Expression result = constants.TryGetValue(node, out field)
                                    ? (field.FieldType == node.Type
                                           ? Expression.MakeMemberAccess(null, field)
                                           : Expression.MakeMemberAccess(Expression.MakeMemberAccess(null, field), field.FieldType.GetField("Value", BindingFlags.Public | BindingFlags.Instance)))
                                    : base.VisitConstant(node);
            if(node.Value is Expression)
            {
                if(closureParameter != null)
                {
                    var exp = (Expression)node.Value;
                    var temp = new ClosureSubstituter(closureParameter, parameters).Visit(exp);
                    if(temp != exp)
                    {
                        var constructor = typeof(ExpressionQuoter).GetConstructor(new[] {typeof(Closure)});
                        result = Expression.Convert(Expression.Call(Expression.New(constructor, closureParameter), typeof(ExpressionVisitor).GetMethod("Visit", new[] {typeof(Expression)}), new[] {result}), node.Type);
                    }
                }
            }
            return result;
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
        private readonly Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches;
    }
}