using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ConstantsExtractor : ExpressionVisitor
    {
        public ConstantExpression[] Extract(Expression exp, bool extractPrimitives = true)
        {
            this.extractPrimitives = extractPrimitives;
            constants = new Dictionary<Expression, int>();
            index = 0;
            Visit(exp);
            return constants.OrderBy(pair => pair.Value).Select(pair => (ConstantExpression)pair.Key).ToArray();
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (extractPrimitives || !node.Type.IsPrimitive && node.Type != typeof(string))
            {
                if(!constants.ContainsKey(node))
                    constants[node] = index++;
            }
            return base.VisitConstant(node);
        }

        private bool extractPrimitives;
        private Dictionary<Expression, int> constants;
        private int index;
    }
}