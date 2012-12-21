using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class AssignExpressionEmitter : ExpressionEmitter<BinaryExpression>
    {
        protected override bool Emit(BinaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
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
                    if(memberExpression.Expression == null) // static member
                    {
                        if(memberExpression.Member is FieldInfo)
                            il.Ldnull();
                        var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                        Type rightType;
                        var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                        if(right.Type == typeof(bool) && rightType == typeof(bool?))
                            context.ConvertFromNullableBoolToBool();
                        if(rightIsNullLabelUsed && context.CanReturn)
                            context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                        using(var temp = context.DeclareLocal(right.Type))
                        {
                            il.Stloc(temp);
                            il.Ldloc(temp);
                            context.EmitMemberAssign(memberExpression.Expression == null ? null : memberExpression.Expression.Type, memberExpression.Member);
                            il.Ldloc(temp);
                        }
                    }
                    else
                    {
                        bool closureAssign = memberExpression.Expression == context.ClosureParameter;
                        var leftIsNullLabel = !closureAssign && context.CanReturn ? il.DefineLabel("leftIsNull") : null;
                        Type type;
                        bool leftIsNullLabelUsed = ExpressionEmittersCollection.Emit(memberExpression.Expression, context, leftIsNullLabel, ResultType.ByRefValueTypesOnly, context.Options.HasFlag(CompilerOptions.ExtendOnAssign), out type);
                        if(type.IsValueType)
                        {
                            using(var temp = context.DeclareLocal(type))
                            {
                                il.Stloc(temp);
                                il.Ldloca(temp);
                            }
                            type = type.MakeByRefType();
                        }
                        if(!closureAssign && context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                            leftIsNullLabelUsed |= context.EmitNullChecking(memberExpression.Expression.Type, leftIsNullLabel);
                        if(leftIsNullLabelUsed)
                            context.EmitReturnDefaultValue(type, leftIsNullLabel, il.DefineLabel("leftIsNotNull"));
                        using(var temp = context.DeclareLocal(type))
                        {
                            il.Stloc(temp);
                            var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                            Type rightType;
                            var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                            if (right.Type == typeof(bool) && rightType == typeof(bool?))
                                context.ConvertFromNullableBoolToBool();
                            if (rightIsNullLabelUsed && context.CanReturn)
                                context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                            using (var value = context.DeclareLocal(right.Type))
                            {
                                il.Stloc(value);
                                il.Ldloc(temp);
                                if(!context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                                {
                                    il.Ldloc(value);
                                    context.EmitMemberAssign(memberExpression.Expression == null ? null : memberExpression.Expression.Type, memberExpression.Member);
                                }
                                else
                                {
                                    var returnValueLabel = il.DefineLabel("returnValue");
                                    il.Brfalse(returnValueLabel);
                                    il.Ldloc(temp);
                                    il.Ldloc(value);
                                    context.EmitMemberAssign(memberExpression.Expression == null ? null : memberExpression.Expression.Type, memberExpression.Member);
                                    il.MarkLabel(returnValueLabel);
                                }
                                il.Ldloc(value);
                            }
                        }
                    }
                    resultType = right.Type;
                    break;
                }
            case ExpressionType.Index:
                {
                    var indexExpression = (IndexExpression)left;
                    if (indexExpression.Object != null && indexExpression.Object.Type.IsArray && indexExpression.Object.Type.GetArrayRank() == 1)
                    {
                        left = Expression.ArrayIndex(indexExpression.Object, indexExpression.Arguments.Single());
                        var binaryExpression = (BinaryExpression)left;
                        var leftIsNullLabel = context.CanReturn ? il.DefineLabel("leftIsNull") : null;
                        Type elementType;
                        bool leftIsNullLabelUsed = ExpressionEmittersCollection.Emit(binaryExpression, context, leftIsNullLabel, ResultType.ByRefAll, context.Options.HasFlag(CompilerOptions.ExtendOnAssign), out elementType);
                        if (context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                            leftIsNullLabelUsed |= context.EmitNullChecking(elementType, leftIsNullLabel);
                        if (leftIsNullLabelUsed)
                            context.EmitReturnDefaultValue(elementType, leftIsNullLabel, il.DefineLabel("leftIsNotNull"));
                        using (var temp = context.DeclareLocal(elementType))
                        {
                            il.Stloc(temp);
                            var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                            Type rightType;
                            var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                            if (right.Type == typeof(bool) && rightType == typeof(bool?))
                                context.ConvertFromNullableBoolToBool();
                            if (rightIsNullLabelUsed && context.CanReturn)
                                context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                            using (var value = context.DeclareLocal(right.Type))
                            {
                                il.Stloc(value);
                                il.Ldloc(temp);
                                if (!context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                                {
                                    il.Ldloc(value);
                                    if (binaryExpression.Type.IsStruct())
                                        context.Il.Stobj(binaryExpression.Type);
                                    else
                                        context.Il.Stind(binaryExpression.Type);
                                }
                                else
                                {
                                    var returnValueLabel = il.DefineLabel("returnValue");
                                    il.Brfalse(returnValueLabel);
                                    il.Ldloc(temp);
                                    il.Ldloc(value);
                                    if (binaryExpression.Type.IsStruct())
                                        context.Il.Stobj(binaryExpression.Type);
                                    else
                                        context.Il.Stind(binaryExpression.Type);
                                    il.MarkLabel(returnValueLabel);
                                }
                                il.Ldloc(value);
                            }
                        }
                    }
                    else
                    {
                        if(indexExpression.Object == null)
                        {
                            if(indexExpression.Indexer == null)
                                throw new InvalidOperationException("Either Object or Indexer should not be null");
                            context.EmitLoadArguments(indexExpression.Arguments.ToArray());
                            var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                            Type rightType;
                            var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                            if (right.Type == typeof(bool) && rightType == typeof(bool?))
                                context.ConvertFromNullableBoolToBool();
                            if (rightIsNullLabelUsed && context.CanReturn)
                                context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                            using (var value = context.DeclareLocal(right.Type))
                            {
                                il.Stloc(value);
                                il.Ldloc(value);
                                MethodInfo setter = indexExpression.Indexer.GetSetMethod(true);
                                if(setter == null)
                                    throw new MissingMethodException(indexExpression.Indexer.ReflectedType.ToString(), "set_" + indexExpression.Indexer.Name);
                                il.Call(setter, indexExpression.Object == null ? null : indexExpression.Object.Type);
                                il.Ldloc(value);
                            }
                        }
                        else
                        {
                            var leftIsNullLabel = context.CanReturn ? il.DefineLabel("leftIsNull") : null;
                            Type type;
                            bool leftIsNullLabelUsed = ExpressionEmittersCollection.Emit(indexExpression.Object, context, leftIsNullLabel, ResultType.ByRefValueTypesOnly, context.Options.HasFlag(CompilerOptions.ExtendOnAssign), out type);
                            if (type.IsValueType)
                            {
                                using (var temp = context.DeclareLocal(type))
                                {
                                    il.Stloc(temp);
                                    il.Ldloca(temp);
                                }
                                type = type.MakeByRefType();
                            }
                            if (context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                                leftIsNullLabelUsed |= context.EmitNullChecking(indexExpression.Object.Type, leftIsNullLabel);
                            if (leftIsNullLabelUsed)
                                context.EmitReturnDefaultValue(type, leftIsNullLabel, il.DefineLabel("leftIsNotNull"));
                            using (var temp = context.DeclareLocal(type))
                            {
                                il.Stloc(temp);
                                var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                                Type rightType;
                                var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                                if (right.Type == typeof(bool) && rightType == typeof(bool?))
                                    context.ConvertFromNullableBoolToBool();
                                if (rightIsNullLabelUsed && context.CanReturn)
                                    context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                                using (var value = context.DeclareLocal(right.Type))
                                {
                                    il.Stloc(value);
                                    il.Ldloc(temp);
                                    if (!context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                                    {
                                        il.Ldloc(value);
                                        EmitIndexAssign(indexExpression, context);
                                    }
                                    else
                                    {
                                        var returnValueLabel = il.DefineLabel("returnValue");
                                        il.Brfalse(returnValueLabel);
                                        il.Ldloc(temp);
                                        il.Ldloc(value);
                                        EmitIndexAssign(indexExpression, context);
                                        il.MarkLabel(returnValueLabel);
                                    }
                                    il.Ldloc(value);
                                }
                            }
                        }
                    }
                    resultType = right.Type;
                    break;
                }
            default:
                throw new InvalidOperationException("Unable to assign to an expression with node type '" + left.NodeType + "'");
            }
            return false;
        }

        private static void EmitIndexAssign(IndexExpression node, EmittingContext context)
        {
            if (node.Indexer != null)
            {
                context.EmitLoadArguments(node.Arguments.ToArray());
                MethodInfo setter = node.Indexer.GetSetMethod(true);
                if (setter == null)
                    throw new MissingMethodException(node.Indexer.ReflectedType.ToString(), "set_" + node.Indexer.Name);
                context.Il.Call(setter);
            }
            else
            {
                Type arrayType = node.Object.Type;
                if (!arrayType.IsArray)
                    throw new InvalidOperationException("An array expected");
                int rank = arrayType.GetArrayRank();
                if (rank != node.Arguments.Count)
                    throw new InvalidOperationException("Incorrect number of indeces '" + node.Arguments.Count + "' provided to access an array with rank '" + rank + "'");
                Type indexType = node.Arguments.First().Type;
                if (indexType != typeof(int) && indexType != typeof(long))
                    throw new InvalidOperationException("Indexing array with an index of type '" + indexType + "' is not allowed");
                context.Il.Ldc_I4(node.Arguments.Count);
                context.Il.Newarr(indexType);
                for (int i = 0; i < node.Arguments.Count; ++i)
                {
                    context.Il.Dup();
                    context.Il.Ldc_I4(i);
                    Type argumentType;
                    context.EmitLoadArgument(node.Arguments[i], false, out argumentType);
                    if (argumentType != indexType)
                        throw new InvalidOperationException("Expected '" + indexType + "' but was '" + argumentType + "'");
                    context.Il.Stelem(indexType);
                }
                MethodInfo setValueMethod = arrayType.GetMethod("SetValue", new[] { arrayType.GetElementType(), indexType.MakeArrayType() });
                if (setValueMethod == null)
                    throw new MissingMethodException(arrayType.ToString(), "SetValue");
                context.Il.Call(setValueMethod);
            }
        }
    }
}