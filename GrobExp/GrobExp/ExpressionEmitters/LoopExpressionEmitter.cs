﻿using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class LoopExpressionEmitter : ExpressionEmitter<LoopExpression>
    {
        protected override bool Emit(LoopExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var il = context.Il;
            GroboIL.Label continueLabel;
            if(node.ContinueLabel == null)
                continueLabel = context.Il.DefineLabel("continue");
            else
            {
                if(!context.Labels.TryGetValue(node.ContinueLabel, out continueLabel))
                    context.Labels.Add(node.ContinueLabel, continueLabel = context.Il.DefineLabel(string.IsNullOrEmpty(node.ContinueLabel.Name) ? "continue" : node.ContinueLabel.Name));
            }
            context.Il.MarkLabel(continueLabel);
            GroboIL.Label breakLabel;
            if(node.BreakLabel == null)
                breakLabel = null;
            else
            {
                if(!context.Labels.TryGetValue(node.BreakLabel, out breakLabel))
                    context.Labels.Add(node.BreakLabel, breakLabel = context.Il.DefineLabel(string.IsNullOrEmpty(node.BreakLabel.Name) ? "break" : node.BreakLabel.Name));
            }
            ExpressionEmittersCollection.Emit(node.Body, context, out resultType);
            il.Br(continueLabel);
            if(breakLabel != null)
                il.MarkLabel(breakLabel);
            return false;
        }
    }
}