using System;
using System.Linq;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class NewArrayBoundsExpressionEmitter : ExpressionEmitter<NewArrayExpression>
    {
        protected override bool Emit(NewArrayExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var il = context.Il;

            GroboIL.Label lengthIsNullLabel = context.CanReturn ? il.DefineLabel("lengthIsNull") : null;
            Type lengthType;
            var labelUsed = ExpressionEmittersCollection.Emit(node.Expressions.Single(), context, lengthIsNullLabel, out lengthType);
            if(!lengthType.IsPrimitive)
                throw new InvalidOperationException("Cannot create an array with length of type '" + lengthType + "'");
            if(labelUsed && context.CanReturn)
            {
                var lengthIsNotNullLabel = il.DefineLabel("lengthIsNotNull");
                il.Br(lengthIsNotNullLabel);
                il.MarkLabel(lengthIsNullLabel);
                il.Pop();
                il.Ldc_I4(0);
                il.MarkLabel(lengthIsNotNullLabel);
            }
            il.Newarr(node.Type.GetElementType());
            resultType = node.Type;
            return false;
        }
    }
}