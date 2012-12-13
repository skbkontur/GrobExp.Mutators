using System;
using System.Linq.Expressions;

namespace GrobExp.ExpressionEmitters
{
    internal abstract class ExpressionEmitter<TExpression> : IExpressionEmitter where TExpression : Expression
    {
        public bool Emit(Expression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            return Emit((TExpression)node, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
        }

        protected abstract bool Emit(TExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType);
    }
}