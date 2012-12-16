using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class DefaultExpressionEmitter : ExpressionEmitter<DefaultExpression>
    {
        protected override bool Emit(DefaultExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            resultType = node.Type;
            if(node.Type != typeof(void))
                context.EmitLoadDefaultValue(node.Type);
            return false;
        }
    }
}