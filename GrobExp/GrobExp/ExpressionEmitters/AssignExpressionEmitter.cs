using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class AssignExpressionEmitter : ExpressionEmitter<BinaryExpression>
    {
        protected override bool Emit(BinaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var il = context.Il;
            var left = node.Left;
            var right = node.Right;
            if(left.Type != right.Type)
                throw new InvalidOperationException("Unable to put object of type '" + right.Type + "' into object of type '" + left.Type + "'");
            switch(left.NodeType)
            {
            case ExpressionType.Parameter:
                {
                    var parameter = (ParameterExpression)left;
                    int index = Array.IndexOf(context.Parameters, parameter);
                    if(index >= 0)
                        il.Ldarga(index); // stack: [&parameter]
                    else
                    {
                        EmittingContext.LocalHolder variable;
                        if(context.VariablesToLocals.TryGetValue(parameter, out variable))
                            il.Ldloca(variable); // stack: [&variable]
                        else
                            throw new InvalidOperationException("Unknown parameter " + parameter);
                    }
                    var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                    Type valueType;
                    bool labelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out valueType); // stack: [address, value]
                    if(right.Type == typeof(bool) && valueType == typeof(bool?))
                        context.ConvertFromNullableBoolToBool();
                    if(labelUsed && context.CanReturn)
                        context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                    il.Stind(left.Type); // *address = value
                    ExpressionEmittersCollection.Emit(parameter, context, out resultType);
                    break;
                }
            case ExpressionType.MemberAccess:
                {
                    var memberExpression = (MemberExpression)left;
                    bool closureAssign = memberExpression.Expression == context.ClosureParameter;
                    var leftIsNullLabel = !closureAssign && context.CanReturn ? il.DefineLabel("leftIsNull") : null;
                    bool leftIsNullLabelUsed = false;
                    if(memberExpression.Expression == null) // static member
                    {
                        if(memberExpression.Member is FieldInfo)
                            il.Ldnull();
                    }
                    else
                    {
                        Type type;
                        leftIsNullLabelUsed = ExpressionEmittersCollection.Emit(memberExpression.Expression, context, leftIsNullLabel, true, context.Options.HasFlag(CompilerOptions.ExtendOnAssign), out type);
                        if(type.IsValueType)
                        {
                            using(var temp = context.DeclareLocal(type))
                            {
                                il.Stloc(temp);
                                il.Ldloca(temp);
                            }
                        }
                        if(!closureAssign && context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                            leftIsNullLabelUsed |= context.EmitNullChecking(memberExpression.Expression.Type, leftIsNullLabel);
                    }
                    using(var temp = context.DeclareLocal(right.Type))
                    {
                        var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                        Type rightType;
                        var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                        if(right.Type == typeof(bool) && rightType == typeof(bool?))
                            context.ConvertFromNullableBoolToBool();
                        if(rightIsNullLabelUsed && context.CanReturn)
                            context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                        il.Stloc(temp);
                        il.Ldloc(temp);
                        context.EmitMemberAssign(memberExpression.Expression == null ? null : memberExpression.Expression.Type, memberExpression.Member);
                        il.Ldloc(temp);
                    }
                    if(leftIsNullLabelUsed)
                    {
                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(leftIsNullLabel);
                        il.Pop();
                        var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                        Type rightType;
                        var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                        if(right.Type == typeof(bool) && rightType == typeof(bool?))
                            context.ConvertFromNullableBoolToBool();
                        if(rightIsNullLabelUsed && context.CanReturn)
                            context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                        il.MarkLabel(doneLabel);
                    }
                    resultType = right.Type;
                    break;
                }
                /*case ExpressionType.ArrayIndex:
                {
                    var binaryExpression = (BinaryExpression)node.Left;
                    var arrayIsNullLabel = context.CanReturn ? il.DefineLabel("arrayIsNull") : null;
                    Type type;
                    bool arrayIsNullLabelUsed = Build(binaryExpression.Left, context, arrayIsNullLabel, true, context.Options.HasFlag(GroboCompilerOptions.ExtendAssigns), out type);
                    if (context.Options.HasFlag(GroboCompilerOptions.CheckNullReferences))
                    {
                        arrayIsNullLabelUsed = true;
                        EmitNullChecking(type, context, arrayIsNullLabel);
                    }

                    GroboIL.Label indexIsNullLabel = context.CanReturn ? il.DefineLabel("indexIsNull") : null;
                    bool labelUsed = Build(node.Right, context, indexIsNullLabel, false, false); // stack: [array, index]
                    if (labelUsed && context.CanReturn)
                    {
                        var indexIsNotNullLabel = il.DefineLabel("indexIsNotNull");
                        il.Br(indexIsNotNullLabel);
                        il.MarkLabel(indexIsNullLabel);
                        il.Pop();
                        il.Ldc_I4(0);
                        il.MarkLabel(indexIsNotNullLabel);
                    }
                    if(context.Options.HasFlag(GroboCompilerOptions.ExtendAssigns))
                    {
                        // stack: [array, index]
                        using(var index = context.DeclareLocal(typeof(int)))
                        {
                            il.Dup(); // stack: [array, index, index]
                            il.Stloc(index); // index = index; stack: [array, index]
                            il.Ldc_I4(0); // stack: [array, index, 0]
                            il.Blt(typeof(int), arrayIsNullLabel); // stack: [array]
                            il.Dup(); // stack: [array, array]
                            il.Ldlen(); // stack: [array, array.Length]
                            il.Ldloc(index); // stack: [array, array.Length, index]
                            var assignLabel = il.DefineLabel("assign");
                            il.Bgt(typeof(int), assignLabel);
                            il.Pop();
                        }
                    }
                    else if (context.Options.HasFlag(GroboCompilerOptions.CheckArrayIndexes))
                    {
                        using (var index = context.DeclareLocal(typeof(int)))
                        {
                            il.Stloc(index); // index = index; stack: [array]
                            il.Dup(); // stack: [array, array]
                            il.Ldlen(); // stack: [array, array.Length]
                            il.Ldloc(index); // stack: [array, array.Length, index]
                            il.Ble(typeof(int), returnDefaultValueLabel); // if(array.Length <= index) goto returnDefaultValue; stack: [array]
                            il.Ldloc(index); // stack: [array, index]
                            il.Ldc_I4(0); // stack: [array, index, 0]
                            il.Blt(typeof(int), returnDefaultValueLabel); // if(index < 0) goto returnDefaultValue; stack: [array]     .
                            il.Ldloc(index); // stack: [array, index]
                        }
                    }
                    break;
                    }*/
            default:
                throw new InvalidOperationException("Unable to assign to an expression with node type '" + left.NodeType + "'");
            }
            return false;
        }
    }
}