using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionCanonizer : ExpressionVisitor
    {
        public ExpressionCanonizer(Expression parametersAccessor)
        {
            this.parametersAccessor = parametersAccessor;
            paramsIndex = 0;
            localParameters = new HashSet<ParameterExpression>();
        }

        public Expression Canonize(Expression expression, out Expression[] parameters)
        {
            var result = Visit(expression);
            parameters = new Expression[hashtable.Count];
            foreach (DictionaryEntry entry in hashtable)
            {
                parameters[(int)entry.Value] = (Expression)entry.Key;
            }
            return result;
        }

        public override Expression Visit(Expression node)
        {
            if (!node.IsLinkOfChain(true, true) || localParameters.Contains(node.SmashToSmithereens()[0]))
            {
                return base.Visit(node);
            }
            return VisitChainOrConsant(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach(var variable in node.Variables)
                localParameters.Add(variable);
            var result = base.VisitBlock(node);
            foreach(var variable in node.Variables)
                localParameters.Remove(variable);
            return result;
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            foreach (var parameter in lambda.Parameters)
                localParameters.Add(parameter);
            var result = base.VisitLambda(lambda);
            foreach (var parameter in lambda.Parameters)
                localParameters.Remove(parameter);
            return result;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            return VisitChainOrConsant(node);
        }

        private Expression VisitChainOrConsant(Expression node)
        {
            if (!hashtable.ContainsKey(node))
            {
                hashtable[node] = paramsIndex++;
            }
            var index = (int)hashtable[node];
            return Expression.Convert(Expression.ArrayIndex(parametersAccessor, Expression.Constant(index, typeof(int))), node.Type);
        }

        private readonly Expression parametersAccessor;
        private int paramsIndex;
        private readonly Hashtable hashtable = new Hashtable();
        private readonly HashSet<ParameterExpression> localParameters;
    }
}
