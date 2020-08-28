using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;

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
            hashCode = new Lazy<int>(() => ExpressionHashCalculator.CalcHashCode(expression, strictly),
                                     LazyThreadSafetyMode.PublicationOnly);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (!(obj is ExpressionWrapper other))
                return false;
            return ExpressionEquivalenceChecker.Equivalent(Expression, other.Expression, strictly, true);
        }

        public override int GetHashCode()
        {
            return hashCode.Value;
        }

        public Expression Expression { get; }

        private readonly bool strictly;

        private readonly Lazy<int> hashCode;
    }
}