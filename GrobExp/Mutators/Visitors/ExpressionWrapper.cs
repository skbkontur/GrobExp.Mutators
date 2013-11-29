using System.Diagnostics;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    [DebuggerDisplay("{Expression}")]
    public class ExpressionWrapper
    {
        public ExpressionWrapper(Expression expression, bool strictly)
        {
            this.expression = expression;
            this.strictly = strictly;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(this, obj))
                return true;
            var other = obj as ExpressionWrapper;
            if(other == null)
                return false;
            return ExpressionEquivalenceChecker.Equivalent(expression, other.expression, strictly);
        }

        public override int GetHashCode()
        {
            return ExpressionHashCalculator.CalcHashCode(expression, strictly);
        }

        public Expression Expression { get { return expression; } }
        private readonly Expression expression;
        private readonly bool strictly;
    }
}