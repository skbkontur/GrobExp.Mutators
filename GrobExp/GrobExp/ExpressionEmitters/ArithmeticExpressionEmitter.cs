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
                var type = left.Type;
                if(type != right.Type)
                    throw new InvalidOperationException("Cannot perform operation '" + node.NodeType + "' on objects of different types '" + left.Type + "' and '" + right.Type + "'");
                if(!type.IsNullable())
                {
                    switch(node.NodeType)
                    {
                    case ExpressionType.Add:
                        il.Add();
                        break;
                    case ExpressionType.Subtract:
                        il.Sub();
                        break;
                    case ExpressionType.Multiply:
                        il.Mul();
                        break;
                    case ExpressionType.Divide:
                        il.Div(type);
                        break;
                    case ExpressionType.Modulo:
                        il.Rem(type);
                        break;
                    default:
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    using(var localLeft = context.DeclareLocal(type))
                    using(var localRight = context.DeclareLocal(type))
                    {
                        il.Stloc(localRight);
                        il.Stloc(localLeft);
                        FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldloca(localLeft);
                        il.Ldfld(hasValueField);
                        il.Ldloca(localRight);
                        il.Ldfld(hasValueField);
                        il.And();
                        var returnNullLabel = il.DefineLabel("returnNull");
                        il.Brfalse(returnNullLabel);
                        FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldloca(localLeft);
                        il.Ldfld(valueField);
                        il.Ldloca(localRight);
                        il.Ldfld(valueField);

                        Type argument = type.GetGenericArguments()[0];
                        switch(node.NodeType)
                        {
                        case ExpressionType.Add:
                            il.Add();
                            break;
                        case ExpressionType.Subtract:
                            il.Sub();
                            break;
                        case ExpressionType.Multiply:
                            il.Mul();
                            break;
                        case ExpressionType.Divide:
                            il.Div(argument);
                            break;
                        case ExpressionType.Modulo:
                            il.Rem(argument);
                            break;
                        default:
                            throw new InvalidOperationException();
                        }
                        il.Newobj(type.GetConstructor(new[] {argument}));

                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(returnNullLabel);
                        il.Ldloca(localLeft);
                        il.Initobj(type);
                        il.Ldloc(localLeft);
                        il.MarkLabel(doneLabel);
                    }
                }
            }
            resultType = node.Type;
            return false;
        }
    }
}