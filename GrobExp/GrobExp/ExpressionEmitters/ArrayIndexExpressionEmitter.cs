using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ArrayIndexExpressionEmitter : ExpressionEmitter<BinaryExpression>
    {
        protected override bool Emit(BinaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var result = false;
            GroboIL il = context.Il;
            Type arrayType;
            result |= ExpressionEmittersCollection.Emit(node.Left, context, returnDefaultValueLabel, true, extend, out arrayType); // stack: [array]
            if(!arrayType.IsArray)
                throw new InvalidOperationException("Unable to perform array index operator to type '" + arrayType + "'");
            if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
            {
                result = true;
                il.Dup(); // stack: [array, array]
                il.Brfalse(returnDefaultValueLabel); // if(array == null) goto returnDefaultValue; stack: [array]
            }
            GroboIL.Label indexIsNullLabel = context.CanReturn ? il.DefineLabel("indexIsNull") : null;
            Type indexType;
            bool labelUsed = ExpressionEmittersCollection.Emit(node.Right, context, indexIsNullLabel, out indexType); // stack: [array, index]
            if(!indexType.IsPrimitive)
                throw new InvalidOperationException("Unable to perform array index operator to type '" + arrayType + "'");
            if(labelUsed && context.CanReturn)
            {
                var indexIsNotNullLabel = il.DefineLabel("indexIsNotNull");
                il.Br(indexIsNotNullLabel);
                il.MarkLabel(indexIsNullLabel);
                il.Pop();
                il.Ldc_I4(0);
                il.MarkLabel(indexIsNotNullLabel);
            }
            if(context.Options.HasFlag(CompilerOptions.CheckArrayIndexes))
            {
                result = true;
                using(var arrayIndex = context.DeclareLocal(typeof(int)))
                {
                    il.Stloc(arrayIndex); // arrayIndex = index; stack: [array]
                    il.Dup(); // stack: [array, array]
                    il.Ldlen(); // stack: [array, array.Length]
                    il.Ldloc(arrayIndex); // stack: [array, array.Length, arrayIndex]
                    il.Ble(typeof(int), returnDefaultValueLabel); // if(array.Length <= arrayIndex) goto returnDefaultValue; stack: [array]
                    il.Ldloc(arrayIndex); // stack: [array, arrayIndex]
                    il.Ldc_I4(0); // stack: [array, arrayIndex, 0]
                    il.Blt(typeof(int), returnDefaultValueLabel); // if(arrayIndex < 0) goto returnDefaultValue; stack: [array]
                    il.Ldloc(arrayIndex); // stack: [array, arrayIndex]
                }
            }
            if(returnByRef && node.Type.IsValueType)
            {
                il.Ldelema(node.Type);
                resultType = node.Type.MakeByRefType();
            }
            else
            {
                il.Ldelem(node.Type); // stack: [array[arrayIndex]]
                resultType = node.Type;
            }
            return result;
        }
    }
}