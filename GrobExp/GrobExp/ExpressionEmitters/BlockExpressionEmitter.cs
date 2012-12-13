using System;
using System.Linq.Expressions;

namespace GrobExp.ExpressionEmitters
{
    internal class BlockExpressionEmitter : ExpressionEmitter<BlockExpression>
    {
        protected override bool Emit(BlockExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            foreach(var variable in node.Variables)
            {
                context.VariablesToLocals.Add(variable, context.DeclareLocal(variable.Type));
                context.Variables.Push(variable);
            }
            resultType = typeof(void);
            for(int index = 0; index < node.Expressions.Count; index++)
            {
                var expression = node.Expressions[index];
                GrobIL il = context.Il;
                var valueIsNullLabel = il.DefineLabel("valueIsNull");
                bool labelUsed = ExpressionEmittersCollection.Emit(expression, context, valueIsNullLabel, out resultType);
                if(resultType != typeof(void) && index < node.Expressions.Count - 1)
                {
                    // eat results of all expressions except the last one
                    if(resultType.IsStruct())
                    {
                        using(var temp = context.DeclareLocal(resultType))
                            context.Il.Stloc(temp);
                    }
                    else context.Il.Pop();
                }
                if(labelUsed)
                {
                    var doneLabel = il.DefineLabel("done");
                    il.Br(doneLabel);
                    il.MarkLabel(valueIsNullLabel);
                    il.Pop();
                    if(resultType != typeof(void) && index == node.Expressions.Count - 1)
                    {
                        // return default value for the last expression in the block
                        context.EmitLoadDefaultValue(resultType);
                    }
                    il.MarkLabel(doneLabel);
                }
            }
            if(node.Type == typeof(bool) && resultType == typeof(bool?))
                context.ConvertFromNullableBoolToBool();
            else if(node.Type == typeof(void) && resultType != typeof(void))
            {
                // eat results of all expressions except the last one
                if(resultType.IsStruct())
                {
                    using(var temp = context.DeclareLocal(resultType))
                        context.Il.Stloc(temp);
                }
                else context.Il.Pop();
            }
            foreach(var variable in node.Variables)
            {
                context.VariablesToLocals[variable].Dispose();
                context.VariablesToLocals.Remove(variable);
                context.Variables.Pop();
            }
            resultType = node.Type;
            return false;
        }
    }
}