using System;
using System.Linq.Expressions;

namespace GrobExp.ExpressionEmitters
{
    internal interface IExpressionEmitter
    {
        bool Emit(Expression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType);
    }
}