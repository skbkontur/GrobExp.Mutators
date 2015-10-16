using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionDependenciesExtractor : ExpressionVisitor
    {
        public ExpressionDependenciesExtractor(Expression parametersAccessor, params ParameterExpression[] namesToExtract )
        {
            this.parametersAccessor = parametersAccessor;
            this.namesToExtract = namesToExtract;
            paramsIndex = 0;
        }

        public Expression ExtractParameters(Expression expression, out Expression[] parameters)
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
            var key = new ExpressionWrapper(node, false);
            var index = hashtable[key];
            if (index == null)
            {
                hashtable[key] = index = paramsIndex++;
            }
            return Expression.Convert(Expression.ArrayIndex(parametersAccessor, Expression.Constant(index, typeof(int))), node.Type);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var key = new ExpressionWrapper(node, false);
            var index = hashtable[key];
            if (index == null)
            {
                hashtable[key] = index = paramsIndex++;
            }
            return Expression.Convert(Expression.ArrayIndex(parametersAccessor, Expression.Constant(index, typeof(int))), node.Type);
        }

        private readonly Expression parametersAccessor;
        private int paramsIndex;
        private readonly Hashtable hashtable = new Hashtable();
        private readonly ParameterExpression[] namesToExtract;
    }
}
