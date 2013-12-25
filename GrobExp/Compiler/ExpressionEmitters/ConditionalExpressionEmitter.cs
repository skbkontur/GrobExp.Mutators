using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.Compiler.ExpressionEmitters
{
    internal class ConditionalExpressionEmitter : ExpressionEmitter<ConditionalExpression>
    {
        protected override bool Emit(ConditionalExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            Expression test = node.Test;
            Expression ifTrue = node.IfTrue;
            Expression ifFalse = node.IfFalse;
            bool ifTrueBranchIsEmpty = ifTrue.NodeType == ExpressionType.Default && ifTrue.Type == typeof(void);
            if(ifTrueBranchIsEmpty)
            {
                test = Expression.Not(test);
                Expression temp = ifTrue;
                ifTrue = ifFalse;
                ifFalse = temp;
            }
            var result = false;
            GroboIL il = context.Il;
            var testIsNullLabel = il.DefineLabel("testIsNull");
            Type testType;
            var testIsNullLabelUsed = ExpressionEmittersCollection.Emit(test, context, testIsNullLabel, out testType);
            if(testType == typeof(bool?))
                context.ConvertFromNullableBoolToBool();
            var ifFalseLabel = il.DefineLabel("ifFalse");
            il.Brfalse(ifFalseLabel);
            Type ifTrueType;
            result |= ExpressionEmittersCollection.Emit(ifTrue, context, returnDefaultValueLabel, whatReturn, extend, out ifTrueType);
            if(node.Type == typeof(void) && ifTrueType != typeof(void))
            {
                using(var temp = context.DeclareLocal(ifTrueType))
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
            result |= ExpressionEmittersCollection.Emit(ifFalse, context, returnDefaultValueLabel, whatReturn, extend, out ifFalseType);
            if(node.Type == typeof(void) && ifFalseType != typeof(void))
            {
                using(var temp = context.DeclareLocal(ifFalseType))
                    il.Stloc(temp);
            }
            il.MarkLabel(doneLabel);
            resultType = node.Type;
            return result;
        }
    }
}