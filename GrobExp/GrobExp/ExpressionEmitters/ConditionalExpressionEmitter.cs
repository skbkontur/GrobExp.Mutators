using System;
using System.Linq.Expressions;

namespace GrobExp.ExpressionEmitters
{
    internal class ConditionalExpressionEmitter : ExpressionEmitter<ConditionalExpression>
    {
        protected override bool Emit(ConditionalExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var result = false;
            GrobIL il = context.Il;
            var testIsNullLabel = il.DefineLabel("testIsNull");
            Type testType;
            var testIsNullLabelUsed = ExpressionEmittersCollection.Emit(node.Test, context, testIsNullLabel, out testType);
            if(testType == typeof(bool?))
                context.ConvertFromNullableBoolToBool();
            var ifFalseLabel = il.DefineLabel("ifFalse");
            il.Brfalse(ifFalseLabel);
            result |= ExpressionEmittersCollection.Emit(node.IfTrue, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
            var doneLabel = il.DefineLabel("done");
            il.Br(doneLabel);
            if(testIsNullLabelUsed)
            {
                il.MarkLabel(testIsNullLabel);
                il.Pop();
            }
            il.MarkLabel(ifFalseLabel);
            result |= ExpressionEmittersCollection.Emit(node.IfFalse, context, returnDefaultValueLabel, returnByRef, extend, out resultType);
            il.MarkLabel(doneLabel);
            return result;
        }
    }
}