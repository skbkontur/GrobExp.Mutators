using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ArithmeticExpressionEmitter : ExpressionEmitter<BinaryExpression>
    {
        protected override bool Emit(BinaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            Expression left = node.Left;
            Expression right = node.Right;
            context.EmitLoadArguments(left, right);
            GroboIL il = context.Il;
            if(node.Method != null)
                il.Call(node.Method);
            else
            {
                if(!left.Type.IsNullable() && !right.Type.IsNullable())
                    EmitOp(il, node.NodeType, node.Type);
                else
                {
                    using(var localLeft = context.DeclareLocal(left.Type))
                    using (var localRight = context.DeclareLocal(right.Type))
                    {
                        il.Stloc(localRight);
                        il.Stloc(localLeft);
                        var returnNullLabel = il.DefineLabel("returnNull");
                        if(left.Type.IsNullable())
                        {
                            il.Ldloca(localLeft);
                            il.Ldfld(left.Type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance));
                            il.Brfalse(returnNullLabel);
                        }
                        if(right.Type.IsNullable())
                        {
                            il.Ldloca(localRight);
                            il.Ldfld(right.Type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance));
                            il.Brfalse(returnNullLabel);
                        }
                        if(!left.Type.IsNullable())
                            il.Ldloc(localLeft);
                        else
                        {
                            il.Ldloca(localLeft);
                            il.Ldfld(left.Type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance));
                        }
                        if(!right.Type.IsNullable())
                            il.Ldloc(localRight);
                        else
                        {
                            il.Ldloca(localRight);
                            il.Ldfld(right.Type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance));
                        }
                        Type argument = node.Type.GetGenericArguments()[0];
                        EmitOp(il, node.NodeType, argument);
                        il.Newobj(node.Type.GetConstructor(new[] { argument }));

                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(returnNullLabel);
                        context.EmitLoadDefaultValue(node.Type);
                        il.MarkLabel(doneLabel);
                    }
                }
            }
            resultType = node.Type;
            return false;
        }

        private static void EmitOp(GroboIL il, ExpressionType nodeType, Type type)
        {
            switch(nodeType)
            {
            case ExpressionType.Add:
                il.Add();
                break;
            case ExpressionType.AddChecked:
                il.Add_Ovf(type);
                break;
            case ExpressionType.Subtract:
                il.Sub();
                break;
            case ExpressionType.SubtractChecked:
                il.Sub_Ovf(type);
                break;
            case ExpressionType.Multiply:
                il.Mul();
                break;
            case ExpressionType.MultiplyChecked:
                il.Mul_Ovf(type);
                break;
            case ExpressionType.Divide:
                il.Div(type);
                break;
            case ExpressionType.Modulo:
                il.Rem(type);
                break;
            case ExpressionType.LeftShift:
                il.Shl();
                break;
            case ExpressionType.RightShift:
                il.Shr(type);
                break;
            default:
                throw new InvalidOperationException();
            }
        }
    }
}