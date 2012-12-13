using System;
using System.Linq.Expressions;

namespace GrobExp.ExpressionEmitters
{
    internal class LabelExpressionEmitter : ExpressionEmitter<LabelExpression>
    {
        protected override bool Emit(LabelExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            bool result = false;
            resultType = typeof(void);
            if(node.DefaultValue != null)
                result = ExpressionEmittersCollection.Emit(node.DefaultValue, context, returnDefaultValueLabel, out resultType);
            GrobIL.Label label;
            if(!context.Labels.TryGetValue(node.Target, out label))
                context.Labels.Add(node.Target, label = context.Il.DefineLabel(node.Target.Name));
            context.Il.MarkLabel(label);
            return result;
        }
    }
}