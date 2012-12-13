using System;
using System.Linq.Expressions;

namespace GrobExp.ExpressionEmitters
{
    internal class GotoExpressionEmitter : ExpressionEmitter<GotoExpression>
    {
        protected override bool Emit(GotoExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            bool result = false;
            resultType = typeof(void);
            if(node.Value != null)
                result = ExpressionEmittersCollection.Emit(node.Value, context, returnDefaultValueLabel, out resultType);
            GrobIL.Label label;
            if(!context.Labels.TryGetValue(node.Target, out label))
                context.Labels.Add(node.Target, label = context.Il.DefineLabel(node.Target.Name));
            context.Il.Br(label);
            return result;
        }
    }
}