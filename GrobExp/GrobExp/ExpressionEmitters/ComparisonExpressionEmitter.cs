using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ComparisonExpressionEmitter : ExpressionEmitter<BinaryExpression>
    {
        protected override bool Emit(BinaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            Expression left = node.Left;
            Expression right = node.Right;
            context.EmitLoadArguments(left, right);
            GroboIL il = context.Il;
            if(node.Method != null)
            {
                if(!left.Type.IsNullable() && !right.Type.IsNullable())
                    il.Call(node.Method);
                else
                {
                    using(var localLeft = context.DeclareLocal(left.Type))
                    using(var localRight = context.DeclareLocal(right.Type))
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
                        il.Call(node.Method);

                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(returnNullLabel);
                        context.EmitLoadDefaultValue(node.Type);
                        il.MarkLabel(doneLabel);
                    }
                }
                resultType = node.Method.ReturnType;
            }
            else
            {
                var type = left.Type;
                if(type != right.Type)
                    throw new InvalidOperationException("Cannot compare objects of different types '" + left.Type + "' and '" + right.Type + "'");
                if(!type.IsNullable())
                {
                    switch(node.NodeType)
                    {
                    case ExpressionType.GreaterThan:
                        il.Cgt(type);
                        break;
                    case ExpressionType.LessThan:
                        il.Clt(type);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        il.Clt(type);
                        il.Ldc_I4(1);
                        il.Xor();
                        break;
                    case ExpressionType.LessThanOrEqual:
                        il.Cgt(type);
                        il.Ldc_I4(1);
                        il.Xor();
                        break;
                    default:
                        throw new InvalidOperationException();
                    }
                    resultType = typeof(bool);
                }
                else
                {
                    if(!context.Options.HasFlag(CompilerOptions.UseTernaryLogic))
                    {
                        using(var localLeft = context.DeclareLocal(type))
                        using(var localRight = context.DeclareLocal(type))
                        {
                            il.Stloc(localRight);
                            il.Stloc(localLeft);
                            il.Ldloca(localLeft);
                            FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                            il.Ldfld(valueField);
                            il.Ldloca(localRight);
                            il.Ldfld(valueField);
                            var returnFalseLabel = il.DefineLabel("returnFalse");

                            Type argument = type.GetGenericArguments()[0];
                            switch(node.NodeType)
                            {
                            case ExpressionType.GreaterThan:
                                il.Ble(argument, returnFalseLabel);
                                break;
                            case ExpressionType.LessThan:
                                il.Bge(argument, returnFalseLabel);
                                break;
                            case ExpressionType.GreaterThanOrEqual:
                                il.Blt(argument, returnFalseLabel);
                                break;
                            case ExpressionType.LessThanOrEqual:
                                il.Bgt(argument, returnFalseLabel);
                                break;
                            default:
                                throw new InvalidOperationException();
                            }
                            il.Ldloca(localLeft);
                            FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                            il.Ldfld(hasValueField);
                            il.Ldloca(localRight);
                            il.Ldfld(hasValueField);
                            il.And();
                            var doneLabel = il.DefineLabel("done");
                            il.Br(doneLabel);
                            il.MarkLabel(returnFalseLabel);
                            il.Ldc_I4(0);
                            il.MarkLabel(doneLabel);
                            resultType = typeof(bool);
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
                            var argumentType = type.GetGenericArguments()[0];

                            switch(node.NodeType)
                            {
                            case ExpressionType.GreaterThan:
                                il.Cgt(argumentType);
                                break;
                            case ExpressionType.LessThan:
                                il.Clt(argumentType);
                                break;
                            case ExpressionType.GreaterThanOrEqual:
                                il.Clt(argumentType);
                                il.Ldc_I4(1);
                                il.Xor();
                                break;
                            case ExpressionType.LessThanOrEqual:
                                il.Cgt(argumentType);
                                il.Ldc_I4(1);
                                il.Xor();
                                break;
                            default:
                                throw new InvalidOperationException();
                            }
                            il.Newobj(nullableBoolConstructor);

                            var doneLabel = il.DefineLabel("done");
                            il.Br(doneLabel);
                            il.MarkLabel(returnNullLabel);
                            context.EmitLoadDefaultValue(typeof(bool?));
                            il.MarkLabel(doneLabel);
                            resultType = typeof(bool?);
                        }
                    }
                }
            }
            return false;
        }

        // ReSharper disable RedundantExplicitNullableCreation
        private static readonly ConstructorInfo nullableBoolConstructor = ((NewExpression)((Expression<Func<bool, bool?>>)(b => new bool?(b))).Body).Constructor;
        // ReSharper restore RedundantExplicitNullableCreation
    }
}