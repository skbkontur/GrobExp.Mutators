using System.Diagnostics;
using System.Linq.Expressions;

using GrobExp.Compiler;

namespace GrobExp.Mutators.Visitors
{
    [DebuggerDisplay("{Expression}")]
    internal class ExpressionWrapper
    {
        public ExpressionWrapper(Expression expression, bool strictly)
        {
            this.Expression = expression;
            this.strictly = strictly;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ExpressionWrapper;
            if (other == null)
                return false;
            return ExpressionEquivalenceChecker.Equivalent(Expression, other.Expression, strictly, true);
        }

        public override int GetHashCode()
        {
            return ExpressionHashCalculator.CalcHashCode(Expression, strictly);
        }

        public Expression Expression { get; }
        private readonly bool strictly;
    }
}