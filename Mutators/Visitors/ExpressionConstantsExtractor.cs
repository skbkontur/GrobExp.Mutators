using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionConstantsExtractor : ExpressionVisitor
    {
        public ExpressionConstantsExtractor(Expression constantsAccessor)
        {
            this.constantsAccessor = constantsAccessor;
        }

        public Expression ExtractConstants(Expression exp, out object[] constants)
        {
            var result = Visit(exp);
            constants = new object[hashtable.Count];
            foreach (DictionaryEntry entry in hashtable)
                constants[(int)entry.Value] = ((KeyValuePair<Type, object>)entry.Key).Value;
            return result;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var key = new KeyValuePair<Type, object>(node.Type, node.Value);
            var index = hashtable[key];
            if (index == null)
            {
                index = constIndex++;
                hashtable[key] = index;
            }

            return Expression.Convert(Expression.ArrayIndex(constantsAccessor, Expression.Constant(index, typeof(int))), node.Type);
        }

        private int constIndex;
        private readonly Expression constantsAccessor;
        private readonly Hashtable hashtable = new Hashtable();
    }
}