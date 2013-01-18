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
            Type leftType, rightType;
            context.EmitLoadArgument(left, false, out leftType);
            context.EmitLoadArgument(right, false, out rightType);
            GroboIL il = context.Il;
            if(node.Method != null)
            {
                if (!leftType.IsNullable() && !rightType.IsNullable())
                    il.Call(node.Method);
                else
                {
                    using(var localLeft = context.DeclareLocal(leftType))
                    using(var localRight = context.DeclareLocal(rightType))
                    {
                        il.Stloc(localRight);
                        il.Stloc(localLeft);
                        var returnNullLabel = il.DefineLabel("returnNull");
                        if(leftType.IsNullable())
                        {
                            il.Ldloca(localLeft);
                            context.EmitHasValueAccess(leftType);
                            il.Brfalse(returnNullLabel);
                        }
                        if(rightType.IsNullable())
                        {
                            il.Ldloca(localRight);
                            context.EmitHasValueAccess(rightType);
                            il.Brfalse(returnNullLabel);
                        }
                        if(!leftType.IsNullable())
                            il.Ldloc(localLeft);
                        else
                        {
                            il.Ldloca(localLeft);
                            context.EmitValueAccess(leftType);
                        }
                        if(!rightType.IsNullable())
                            il.Ldloc(localRight);
                        else
                        {
                            il.Ldloca(localRight);
                            context.EmitValueAccess(rightType);
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
                var type = leftType;
                if(type != rightType)
                    throw new InvalidOperationException("Cannot compare objects of different types '" + leftType + "' and '" + rightType + "'");
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
                            context.EmitValueAccess(type);
                            il.Ldloca(localRight);
                            context.EmitValueAccess(type);
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
                            context.EmitHasValueAccess(type);
                            il.Ldloca(localRight);
                            context.EmitHasValueAccess(type);
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
                            il.Ldloca(localLeft);
                            context.EmitHasValueAccess(type);
                            il.Ldloca(localRight);
                            context.EmitHasValueAccess(type);
                            il.And();
                            var returnNullLabel = il.DefineLabel("returnNull");
                            il.Brfalse(returnNullLabel);
                            il.Ldloca(localLeft);
                            context.EmitValueAccess(type);
                            il.Ldloca(localRight);
                            context.EmitValueAccess(type);
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