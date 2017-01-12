using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExpressionReplacer : ExpressionVisitor
    {
        public ExpressionReplacer(Dictionary<Expression, Expression> replacements)
        {
            this.replacements = replacements;
        }

        public override Expression Visit(Expression node)
        {
            Expression replacement;
            return node != null && replacements.TryGetValue(node, out replacement) ? replacement : base.Visit(node);
        }

        private readonly Dictionary<Expression, Expression> replacements;
    }
}