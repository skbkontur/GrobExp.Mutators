﻿using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class TryExpressionEmitter : ExpressionEmitter<TryExpression>
    {
        protected override bool Emit(TryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var il = context.Il;
            il.BeginExceptionBlock();
            returnDefaultValueLabel = context.CanReturn ? il.DefineLabel("returnDefaultValue") : null;
            bool returnDefaultValueLabelUsed = ExpressionEmittersCollection.Emit(node.Body, context, returnDefaultValueLabel, whatReturn, extend, out resultType);
            EmittingContext.LocalHolder retValue = null;
            var doneLabel = il.DefineLabel("done");
            if(resultType == typeof(void))
            {
                if(returnDefaultValueLabelUsed)
                    il.MarkLabel(returnDefaultValueLabel);
                il.Leave(doneLabel);
            }
            else
            {
                retValue = context.DeclareLocal(resultType);
                il.Stloc(retValue);
                il.Leave(doneLabel);
                if(returnDefaultValueLabelUsed)
                {
                    il.MarkLabel(returnDefaultValueLabel);
                    if(resultType.IsValueType)
                    {
                        il.Ldloca(retValue);
                        il.Initobj(resultType);
                    }
                    else
                    {
                        il.Ldnull(resultType);
                        il.Stloc(retValue);
                    }
                    il.Leave(doneLabel);
                }
            }
            foreach(var catchBlock in node.Handlers)
            {
                if(catchBlock.Filter == null)
                {
                    il.BeginCatchBlock(catchBlock.Test);
                    if(catchBlock.Variable == null)
                        il.Pop();
                    else
                    {
                        // todo вызвать ф-цию из AssignExpressionEmitter
                        var index = Array.IndexOf(context.Parameters, catchBlock.Variable);
                        if(index >= 0)
                            il.Starg(index);
                        else
                        {
                            EmittingContext.LocalHolder variable;
                            if(context.VariablesToLocals.TryGetValue(catchBlock.Variable, out variable))
                                il.Stloc(variable);
                            else
                                throw new InvalidOperationException("Unknown parameter " + catchBlock.Variable);
                        }
                    }
                }
                else
                {
                    il.BeginExceptFilterBlock();
                    il.Isinst(catchBlock.Test);
                    il.Dup();
                    var rightTypeLabel = il.DefineLabel("rightType");
                    il.Brtrue(rightTypeLabel);
                    il.Pop();
                    il.Ldc_I4(0);
                    var endFilterLabel = il.DefineLabel("endFilter");
                    il.Br(endFilterLabel);
                    il.MarkLabel(rightTypeLabel);
                    if(catchBlock.Variable == null)
                        il.Pop();
                    else
                    {
                        // todo вызвать ф-цию из AssignExpressionEmitter
                        var index = Array.IndexOf(context.Parameters, catchBlock.Variable);
                        if(index >= 0)
                            il.Starg(index);
                        else
                        {
                            EmittingContext.LocalHolder variable;
                            if(context.VariablesToLocals.TryGetValue(catchBlock.Variable, out variable))
                                il.Stloc(variable);
                            else
                                throw new InvalidOperationException("Unknown parameter " + catchBlock.Variable);
                        }
                    }
                    GroboIL.Label returnFalseLabel = context.CanReturn ? il.DefineLabel("returnFalse") : null;
                    Type filterResultType;
                    bool returnFalseLabelUsed = ExpressionEmittersCollection.Emit(catchBlock.Filter, context, returnFalseLabel, out filterResultType);
                    if(returnFalseLabelUsed)
                    {
                        il.Br(endFilterLabel);
                        il.MarkLabel(returnFalseLabel);
                        il.Pop();
                        il.Ldc_I4(0);
                    }
                    il.MarkLabel(endFilterLabel);
                    il.BeginCatchBlock(null);
                    il.Pop();
                }

                context.EmitLoadArguments(catchBlock.Body);
                if(catchBlock.Body.Type != typeof(void))
                    il.Stloc(retValue);
            }

            if(node.Fault != null)
            {
                il.BeginFaultBlock();
                EmitExpressionAsVoid(node.Fault, context);
            }

            if(node.Finally != null)
            {
                il.BeginFinallyBlock();
                EmitExpressionAsVoid(node.Finally, context);
            }

            il.EndExceptionBlock();

            il.MarkLabel(doneLabel);
            if(retValue != null)
            {
                il.Ldloc(retValue);
                retValue.Dispose();
            }
            return false;
        }

        // todo обобщить
        private static void EmitExpressionAsVoid(Expression node, EmittingContext context)
        {
            var il = context.Il;
            GroboIL.Label skipLabel = context.CanReturn ? il.DefineLabel("skip") : null;
            Type resultType;
            bool skipLabelUsed = ExpressionEmittersCollection.Emit(node, context, skipLabel, ResultType.Void, false, out resultType);
            if(skipLabelUsed)
            {
                var endLabel = il.DefineLabel("end");
                if(resultType != typeof(void))
                {
                    if(!resultType.IsStruct())
                        il.Pop();
                    else
                    {
                        using(var temp = context.DeclareLocal(resultType))
                            il.Stloc(temp);
                    }
                }
                il.Br(endLabel);
                il.MarkLabel(skipLabel);
                il.Pop();
                il.MarkLabel(endLabel);
            }
        }
    }
}