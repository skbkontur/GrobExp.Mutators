using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ConditionalExpressionEmitter : ExpressionEmitter<ConditionalExpression>
    {
        protected override bool Emit(ConditionalExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var result = false;
            GroboIL il = context.Il;
            var testIsNullLabel = il.DefineLabel("testIsNull");
            Type testType;
            var testIsNullLabelUsed = ExpressionEmittersCollection.Emit(node.Test, context, testIsNullLabel, out testType);
            if(testType == typeof(bool?))
                context.ConvertFromNullableBoolToBool();
            var ifFalseLabel = il.DefineLabel("ifFalse");
            il.Brfalse(ifFalseLabel);
            Type ifTrueType;
            result |= ExpressionEmittersCollection.Emit(node.IfTrue, context, returnDefaultValueLabel, whatReturn, extend, out ifTrueType);
            if (node.Type == typeof(void) && ifTrueType != typeof(void))
            {
                using (var temp = context.DeclareLocal(ifTrueType))
                    il.Stloc(temp);
            }
            var doneLabel = il.DefineLabel("done");
            il.Br(doneLabel);
            if(testIsNullLabelUsed)
            {
                il.MarkLabel(testIsNullLabel);
                il.Pop();
            }
            il.MarkLabel(ifFalseLabel);
            Type ifFalseType;
            result |= ExpressionEmittersCollection.Emit(node.IfFalse, context, returnDefaultValueLabel, whatReturn, extend, out ifFalseType);
            if (node.Type == typeof(void) && ifFalseType != typeof(void))
            {
                using (var temp = context.DeclareLocal(ifFalseType))
                    il.Stloc(temp);
            }
            il.MarkLabel(doneLabel);
            resultType = node.Type;
            return result;
        }
    }
}