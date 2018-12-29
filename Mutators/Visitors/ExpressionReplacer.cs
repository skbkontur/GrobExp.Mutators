using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    internal class ExpressionReplacer : ExpressionVisitor
    {
        public ExpressionReplacer(Dictionary<Expression, Expression> replacements)
        {
            this.replacements = replacements;
        }

        public override Expression Visit(Expression node)
        {
            return node != null && replacements.TryGetValue(node, out var replacement) ? replacement : base.Visit(node);
        }

        private readonly Dictionary<Expression, Expression> replacements;
    }
}