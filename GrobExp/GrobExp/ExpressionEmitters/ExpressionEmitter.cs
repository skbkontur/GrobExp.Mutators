using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal abstract class ExpressionEmitter<TExpression> : IExpressionEmitter where TExpression : Expression
    {
        public bool Emit(Expression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            return Emit((TExpression)node, context, returnDefaultValueLabel, whatReturn, extend, out resultType);
        }

        protected abstract bool Emit(TExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType);
    }
}