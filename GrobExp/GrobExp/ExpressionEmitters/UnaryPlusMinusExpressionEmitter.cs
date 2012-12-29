using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class UnaryPlusMinusExpressionEmitter : ExpressionEmitter<UnaryExpression>
    {
        protected override bool Emit(UnaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            Type operandType;
            var result = ExpressionEmittersCollection.Emit(node.Operand, context, returnDefaultValueLabel, whatReturn, extend, out operandType);
            GroboIL il = context.Il;
            switch(node.NodeType)
            {
            case ExpressionType.UnaryPlus:
                if(node.Method != null)
                    il.Call(node.Method);
                break;
            case ExpressionType.Negate:
                if(node.Method != null)
                    il.Call(node.Method);
                else
                {
                    if(!operandType.IsNullable())
                        il.Neg();
                    else
                    {
                        using(var temp = context.DeclareLocal(operandType))
                        {
                            il.Stloc(temp);
                            il.Ldloca(temp);
                            il.Ldfld(operandType.GetField("hasValue", BindingFlags.Instance | BindingFlags.NonPublic));
                            var returnNullLabel = il.DefineLabel("returnLabel");
                            il.Brfalse(returnNullLabel);
                            il.Ldloca(temp);
                            il.Ldfld(operandType.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic));
                            il.Neg();
                            il.Newobj(operandType.GetConstructor(new[] {operandType.GetGenericArguments()[0]}));
                            var doneLabel = il.DefineLabel("done");
                            il.Br(doneLabel);
                            il.MarkLabel(returnNullLabel);
                            context.EmitLoadDefaultValue(operandType);
                            il.MarkLabel(doneLabel);
                        }
                    }
                }
                break;
            case ExpressionType.NegateChecked:
                if(node.Method != null)
                    il.Call(node.Method);
                else
                {
                    if(!operandType.IsNullable())
                    {
                        using(var temp = context.DeclareLocal(operandType))
                        {
                            il.Stloc(temp);
                            il.Ldc_I4(0);
                            context.EmitConvert(typeof(int), operandType);
                            il.Ldloc(temp);
                            il.Sub_Ovf(operandType);
                        }
                    }
                    else
                    {
                        using(var temp = context.DeclareLocal(operandType))
                        {
                            il.Stloc(temp);
                            il.Ldloca(temp);
                            il.Ldfld(operandType.GetField("hasValue", BindingFlags.Instance | BindingFlags.NonPublic));
                            var returnNullLabel = il.DefineLabel("returnLabel");
                            il.Brfalse(returnNullLabel);
                            var argumentType = operandType.GetGenericArguments()[0];
                            il.Ldc_I4(0);
                            context.EmitConvert(typeof(int), argumentType);
                            il.Ldloca(temp);
                            il.Ldfld(operandType.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic));
                            il.Sub_Ovf(argumentType);
                            il.Newobj(operandType.GetConstructor(new[] {argumentType}));
                            var doneLabel = il.DefineLabel("done");
                            il.Br(doneLabel);
                            il.MarkLabel(returnNullLabel);
                            context.EmitLoadDefaultValue(operandType);
                            il.MarkLabel(doneLabel);
                        }
                    }
                }
                break;
            default:
                throw new InvalidOperationException("Node type '" + node.NodeType + "' invalid at this point");
            }
            resultType = node.Type;
            return result;
        }
    }
}