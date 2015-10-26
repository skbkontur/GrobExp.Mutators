using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionCanonizer : ExpressionVisitor
    {
        public ExpressionCanonizer(Expression parametersAccessor, params ParameterExpression[] namesToExtract )
        {
            this.parametersAccessor = parametersAccessor;
            this.namesToExtract = namesToExtract;
            paramsIndex = 0;
        }

        public Expression Canonize(Expression expression, out Expression[] parameters)
        {
            var result = Visit(expression);
            parameters = new Expression[hashtable.Count];
            foreach (DictionaryEntry entry in hashtable)
            {
                parameters[(int)entry.Value] = ((ExpressionWrapper)entry.Key).Expression;
            }
            return result;
        }

        public override Expression Visit(Expression node)
        {
            if (!node.IsLinkOfChain(true, true) || !namesToExtract.Contains((ParameterExpression)node.SmashToSmithereens()[0]))
            {
                return base.Visit(node);
            }
            return VisitChainOrConsant(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            return VisitChainOrConsant(node);
        }

        private Expression VisitChainOrConsant(Expression node)
        {
            var key = new ExpressionWrapper(node, false);
            if (!hashtable.ContainsKey(key))
            {
                hashtable[key] = paramsIndex++;
            }
            var index = (int)hashtable[key];
            return Expression.Convert(Expression.ArrayIndex(parametersAccessor, Expression.Constant(index, typeof(int))), node.Type);
        }

        private readonly Expression parametersAccessor;
        private int paramsIndex;
        private readonly Hashtable hashtable = new Hashtable();
        private readonly ParameterExpression[] namesToExtract;
    }
}
