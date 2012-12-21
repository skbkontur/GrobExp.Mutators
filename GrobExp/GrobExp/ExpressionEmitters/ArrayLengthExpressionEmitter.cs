using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ArrayLengthExpressionEmitter : ExpressionEmitter<UnaryExpression>
    {
        protected override bool Emit(UnaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var il = context.Il;
            Type arrayType;
            var result = ExpressionEmittersCollection.Emit(node.Operand, context, returnDefaultValueLabel, out arrayType);
            if(!arrayType.IsArray)
                throw new InvalidOperationException("Unable to perform array index operator to type '" + arrayType + "'");
            if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
            {
                result = true;
                il.Dup();
                il.Brfalse(returnDefaultValueLabel);
            }
            il.Ldlen();
            resultType = typeof(int);
            return result;
        }
    }
}