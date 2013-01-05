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
            bool result = false;
            if(left.Type != right.Type)
                throw new InvalidOperationException("Unable to put object of type '" + right.Type + "' into object of type '" + left.Type + "'");
            switch(left.NodeType)
            {
            case ExpressionType.Parameter:
                {
                    Type parameterType;
                    ExpressionEmittersCollection.Emit(left, context, null, ResultType.ByRefAll, false, out parameterType);
                    var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                    Type valueType;
                    bool labelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out valueType); // stack: [address, value]
                    if(right.Type == typeof(bool) && valueType == typeof(bool?))
                        context.ConvertFromNullableBoolToBool();
                    if(labelUsed && context.CanReturn)
                        context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                    using(var value = context.DeclareLocal(right.Type))
                    {
                        il.Stloc(value);

                        switch(node.NodeType)
                        {
                        case ExpressionType.Assign:
                            il.Ldloc(value);
                            il.Stind(left.Type); // *address = value
                            il.Ldloc(value);
                            break;
                        case ExpressionType.AddAssign:
                        case ExpressionType.AddAssignChecked:
                        case ExpressionType.SubtractAssign:
                        case ExpressionType.SubtractAssignChecked:
                        case ExpressionType.MultiplyAssign:
                        case ExpressionType.MultiplyAssignChecked:
                        case ExpressionType.DivideAssign:
                            il.Dup(); // stack: [&parameter, &parameter]
                            il.Ldind(left.Type); // stack: [&parameter, parameter]
                            il.Ldloc(value); // stack: [&parameter, parameter, value]
                            context.EmitArithmeticOperation(GetOp(node.NodeType), left.Type, left.Type, right.Type, node.Method); // stack: [&parameter, result]
                            using(var temp = context.DeclareLocal(left.Type))
                            {
                                il.Stloc(temp);
                                il.Ldloc(temp);
                                il.Stind(left.Type);
                                il.Ldloc(temp);
                            }
                            break;
                        default:
                            throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                        }
                    }
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
                        using(var value = context.DeclareLocal(right.Type))
                        {
                            il.Stloc(value);
                            switch(node.NodeType)
                            {
                            case ExpressionType.Assign:
                                il.Ldloc(value);
                                context.EmitMemberAssign(null, memberExpression.Member);
                                il.Ldloc(value);
                                break;
                            case ExpressionType.AddAssign:
                            case ExpressionType.AddAssignChecked:
                            case ExpressionType.SubtractAssign:
                            case ExpressionType.SubtractAssignChecked:
                            case ExpressionType.MultiplyAssign:
                            case ExpressionType.MultiplyAssignChecked:
                            case ExpressionType.DivideAssign:
                                if (memberExpression.Member is FieldInfo)
                                    il.Dup(); // stack: [owner, owner]
                                Type memberType;
                                context.EmitMemberAccess(null, memberExpression.Member, ResultType.Value, out memberType);
                                il.Ldloc(value);
                                context.EmitArithmeticOperation(GetOp(node.NodeType), memberType, memberType, right.Type, node.Method);
                                using(var temp = context.DeclareLocal(left.Type))
                                {
                                    il.Stloc(temp);
                                    il.Ldloc(temp);
                                    context.EmitMemberAssign(null, memberExpression.Member);
                                    il.Ldloc(temp);
                                }
                                break;
                            default:
                                throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                            }
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
                        using(var owner = context.DeclareLocal(type))
                        {
                            il.Stloc(owner);
                            var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                            Type rightType;
                            var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                            if(right.Type == typeof(bool) && rightType == typeof(bool?))
                                context.ConvertFromNullableBoolToBool();
                            if(rightIsNullLabelUsed && context.CanReturn)
                                context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                            using(var value = context.DeclareLocal(right.Type))
                            {
                                il.Stloc(value);
                                il.Ldloc(owner);
                                if(!context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                                {
                                    switch(node.NodeType)
                                    {
                                    case ExpressionType.Assign:
                                        il.Ldloc(value);
                                        context.EmitMemberAssign(memberExpression.Expression.Type, memberExpression.Member);
                                        il.Ldloc(value);
                                        break;
                                    case ExpressionType.AddAssign:
                                    case ExpressionType.AddAssignChecked:
                                    case ExpressionType.SubtractAssign:
                                    case ExpressionType.SubtractAssignChecked:
                                    case ExpressionType.MultiplyAssign:
                                    case ExpressionType.MultiplyAssignChecked:
                                    case ExpressionType.DivideAssign:
                                        il.Dup(); // stack: [owner, owner]
                                        Type memberType;
                                        context.EmitMemberAccess(null, memberExpression.Member, ResultType.Value, out memberType);
                                        il.Ldloc(value);
                                        context.EmitArithmeticOperation(GetOp(node.NodeType), memberType, memberType, right.Type, node.Method);
                                        using(var temp = context.DeclareLocal(left.Type))
                                        {
                                            il.Stloc(temp);
                                            il.Ldloc(temp);
                                            context.EmitMemberAssign(null, memberExpression.Member);
                                            il.Ldloc(temp);
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                                    }
                                }
                                else
                                {
                                    switch(node.NodeType)
                                    {
                                    case ExpressionType.Assign:
                                        var returnValueLabel = il.DefineLabel("returnValue");
                                        il.Brfalse(returnValueLabel);
                                        il.Ldloc(owner);
                                        il.Ldloc(value);
                                        context.EmitMemberAssign(memberExpression.Expression.Type, memberExpression.Member);
                                        il.MarkLabel(returnValueLabel);
                                        il.Ldloc(value);
                                        break;
                                    case ExpressionType.AddAssign:
                                    case ExpressionType.AddAssignChecked:
                                    case ExpressionType.SubtractAssign:
                                    case ExpressionType.SubtractAssignChecked:
                                    case ExpressionType.MultiplyAssign:
                                    case ExpressionType.MultiplyAssignChecked:
                                    case ExpressionType.DivideAssign:
                                        il.Dup(); // stack: [owner, owner]
                                        il.Brfalse(returnDefaultValueLabel);
                                        result = true;
                                        il.Dup(); // stack: [owner, owner]
                                        Type memberType;
                                        context.EmitMemberAccess(memberExpression.Expression.Type, memberExpression.Member, ResultType.Value, out memberType);
                                        il.Ldloc(value);
                                        context.EmitArithmeticOperation(GetOp(node.NodeType), memberType, memberType, right.Type, node.Method);
                                        using(var temp = context.DeclareLocal(left.Type))
                                        {
                                            il.Stloc(temp);
                                            il.Ldloc(temp);
                                            context.EmitMemberAssign(memberExpression.Expression.Type, memberExpression.Member);
                                            il.Ldloc(temp);
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            case ExpressionType.Index:
                {
                    var indexExpression = (IndexExpression)left;
                    if(indexExpression.Object != null && indexExpression.Object.Type.IsArray && indexExpression.Object.Type.GetArrayRank() == 1)
                    {
                        left = Expression.ArrayIndex(indexExpression.Object, indexExpression.Arguments.Single());
                        var binaryExpression = (BinaryExpression)left;
                        var leftIsNullLabel = context.CanReturn ? il.DefineLabel("leftIsNull") : null;
                        Type elementType;
                        bool leftIsNullLabelUsed = ExpressionEmittersCollection.Emit(binaryExpression, context, leftIsNullLabel, ResultType.ByRefAll, context.Options.HasFlag(CompilerOptions.ExtendOnAssign), out elementType);
                        if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                            leftIsNullLabelUsed |= context.EmitNullChecking(elementType, leftIsNullLabel);
                        if(leftIsNullLabelUsed)
                            context.EmitReturnDefaultValue(elementType, leftIsNullLabel, il.DefineLabel("leftIsNotNull"));
                        using(var itemAddress = context.DeclareLocal(elementType))
                        {
                            il.Stloc(itemAddress);
                            var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                            Type rightType;
                            var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                            if(right.Type == typeof(bool) && rightType == typeof(bool?))
                                context.ConvertFromNullableBoolToBool();
                            if(rightIsNullLabelUsed && context.CanReturn)
                                context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                            using(var value = context.DeclareLocal(right.Type))
                            {
                                il.Stloc(value);
                                il.Ldloc(itemAddress);
                                if(!context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                                {
                                    switch(node.NodeType)
                                    {
                                    case ExpressionType.Assign:
                                        il.Ldloc(value);
                                        il.Stind(binaryExpression.Type);
                                        il.Ldloc(value);
                                        break;
                                    case ExpressionType.AddAssign:
                                    case ExpressionType.AddAssignChecked:
                                    case ExpressionType.SubtractAssign:
                                    case ExpressionType.SubtractAssignChecked:
                                    case ExpressionType.MultiplyAssign:
                                    case ExpressionType.MultiplyAssignChecked:
                                    case ExpressionType.DivideAssign:
                                        il.Dup(); // stack: [owner, owner]
                                        il.Ldind(binaryExpression.Type);
                                        il.Ldloc(value);
                                        context.EmitArithmeticOperation(GetOp(node.NodeType), binaryExpression.Type, binaryExpression.Type, right.Type, node.Method);
                                        using(var temp = context.DeclareLocal(binaryExpression.Type))
                                        {
                                            il.Stloc(temp);
                                            il.Ldloc(temp);
                                            il.Stind(binaryExpression.Type);
                                            il.Ldloc(temp);
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                                    }
                                }
                                else
                                {
                                    switch(node.NodeType)
                                    {
                                    case ExpressionType.Assign:
                                        var returnValueLabel = il.DefineLabel("returnValue");
                                        il.Brfalse(returnValueLabel);
                                        il.Ldloc(itemAddress);
                                        il.Ldloc(value);
                                        il.Stind(binaryExpression.Type);
                                        il.MarkLabel(returnValueLabel);
                                        il.Ldloc(value);
                                        break;
                                    case ExpressionType.AddAssign:
                                    case ExpressionType.AddAssignChecked:
                                    case ExpressionType.SubtractAssign:
                                    case ExpressionType.SubtractAssignChecked:
                                    case ExpressionType.MultiplyAssign:
                                    case ExpressionType.MultiplyAssignChecked:
                                    case ExpressionType.DivideAssign:
                                        il.Dup(); // stack: [itemAddress, itemAddress]
                                        il.Brfalse(returnDefaultValueLabel);
                                        result = true;
                                        il.Dup(); // stack: [itemAddress, itemAddress]
                                        il.Ldind(binaryExpression.Type);
                                        il.Ldloc(value);
                                        context.EmitArithmeticOperation(GetOp(node.NodeType), binaryExpression.Type, binaryExpression.Type, right.Type, node.Method);
                                        using(var temp = context.DeclareLocal(binaryExpression.Type))
                                        {
                                            il.Stloc(temp);
                                            il.Ldloc(temp);
                                            il.Stind(binaryExpression.Type);
                                            il.Ldloc(temp);
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if(indexExpression.Object == null)
                            throw new InvalidOperationException("Indexing of null object is invalid");
                        var leftIsNullLabel = context.CanReturn ? il.DefineLabel("leftIsNull") : null;
                        Type type;
                        bool leftIsNullLabelUsed = ExpressionEmittersCollection.Emit(indexExpression.Object, context, leftIsNullLabel, ResultType.ByRefValueTypesOnly, context.Options.HasFlag(CompilerOptions.ExtendOnAssign), out type);
                        if(type.IsValueType)
                        {
                            using(var temp = context.DeclareLocal(type))
                            {
                                il.Stloc(temp);
                                il.Ldloca(temp);
                            }
                            type = type.MakeByRefType();
                        }
                        if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                            leftIsNullLabelUsed |= context.EmitNullChecking(indexExpression.Object.Type, leftIsNullLabel);
                        if(leftIsNullLabelUsed)
                            context.EmitReturnDefaultValue(type, leftIsNullLabel, il.DefineLabel("leftIsNotNull"));
                        using(var owner = context.DeclareLocal(type))
                        {
                            il.Stloc(owner);
                            var rightIsNullLabel = context.CanReturn ? il.DefineLabel("rightIsNull") : null;
                            Type rightType;
                            var rightIsNullLabelUsed = ExpressionEmittersCollection.Emit(right, context, rightIsNullLabel, out rightType);
                            if(right.Type == typeof(bool) && rightType == typeof(bool?))
                                context.ConvertFromNullableBoolToBool();
                            if(rightIsNullLabelUsed && context.CanReturn)
                                context.EmitReturnDefaultValue(right.Type, rightIsNullLabel, il.DefineLabel("rightIsNotNull"));
                            using(var value = context.DeclareLocal(right.Type))
                            {
                                il.Stloc(value);
                                il.Ldloc(owner);
                                if(!context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                                {
                                    switch(node.NodeType)
                                    {
                                    case ExpressionType.Assign:
                                        EmitIndexAssign(indexExpression, context, value);
                                        il.Ldloc(value);
                                        break;
                                    case ExpressionType.AddAssign:
                                    case ExpressionType.AddAssignChecked:
                                    case ExpressionType.SubtractAssign:
                                    case ExpressionType.SubtractAssignChecked:
                                    case ExpressionType.MultiplyAssign:
                                    case ExpressionType.MultiplyAssignChecked:
                                    case ExpressionType.DivideAssign:
                                        il.Dup(); // stack: [owner, owner]
                                        EmitIndexAccess(indexExpression, context);
                                        il.Ldloc(value);
                                        context.EmitArithmeticOperation(GetOp(node.NodeType), indexExpression.Type, indexExpression.Type, right.Type, node.Method);
                                        using(var temp = context.DeclareLocal(indexExpression.Type))
                                        {
                                            il.Stloc(temp);
                                            EmitIndexAssign(indexExpression, context, temp);
                                            il.Ldloc(temp);
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                                    }
                                }
                                else
                                {
                                    switch(node.NodeType)
                                    {
                                    case ExpressionType.Assign:
                                        var returnValueLabel = il.DefineLabel("returnValue");
                                        il.Brfalse(returnValueLabel);
                                        il.Ldloc(owner);
                                        EmitIndexAssign(indexExpression, context, value);
                                        il.MarkLabel(returnValueLabel);
                                        il.Ldloc(value);
                                        break;
                                    case ExpressionType.AddAssign:
                                    case ExpressionType.AddAssignChecked:
                                    case ExpressionType.SubtractAssign:
                                    case ExpressionType.SubtractAssignChecked:
                                    case ExpressionType.MultiplyAssign:
                                    case ExpressionType.MultiplyAssignChecked:
                                    case ExpressionType.DivideAssign:
                                        il.Dup(); // stack: [itemAddress, itemAddress]
                                        il.Brfalse(returnDefaultValueLabel);
                                        result = true;
                                        il.Dup(); // stack: [itemAddress, itemAddress]
                                        EmitIndexAccess(indexExpression, context);
                                        il.Ldloc(value);
                                        context.EmitArithmeticOperation(GetOp(node.NodeType), indexExpression.Type, indexExpression.Type, right.Type, node.Method);
                                        using(var temp = context.DeclareLocal(indexExpression.Type))
                                        {
                                            il.Stloc(temp);
                                            EmitIndexAssign(indexExpression, context, temp);
                                            il.Ldloc(temp);
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            default:
                throw new InvalidOperationException("Unable to assign to an expression with node type '" + left.NodeType + "'");
            }
            resultType = right.Type;
            return result;
        }

        private static ExpressionType GetOp(ExpressionType nodeType)
        {
            switch(nodeType)
            {
            case ExpressionType.AddAssign:
                return ExpressionType.Add;
            case ExpressionType.AddAssignChecked:
                return ExpressionType.AddChecked;
            case ExpressionType.SubtractAssign:
                return ExpressionType.Subtract;
            case ExpressionType.SubtractAssignChecked:
                return ExpressionType.SubtractChecked;
            case ExpressionType.MultiplyAssign:
                return ExpressionType.Multiply;
            case ExpressionType.MultiplyAssignChecked:
                return ExpressionType.MultiplyChecked;
            case ExpressionType.DivideAssign:
                return ExpressionType.Divide;
            default:
                throw new NotSupportedException("Unable to extract operation type from node type '" + nodeType + "'");
            }
        }

        private static void EmitIndexAssign(IndexExpression node, EmittingContext context, EmittingContext.LocalHolder value)
        {
            if(node.Indexer != null)
            {
                context.EmitLoadArguments(node.Arguments.ToArray());
                context.Il.Ldloc(value);
                MethodInfo setter = node.Indexer.GetSetMethod(true);
                if(setter == null)
                    throw new MissingMethodException(node.Indexer.ReflectedType.ToString(), "set_" + node.Indexer.Name);
                context.Il.Call(setter);
            }
            else
            {
                Type arrayType = node.Object.Type;
                if(!arrayType.IsArray)
                    throw new InvalidOperationException("An array expected");
                int rank = arrayType.GetArrayRank();
                if(rank != node.Arguments.Count)
                    throw new InvalidOperationException("Incorrect number of indeces '" + node.Arguments.Count + "' provided to access an array with rank '" + rank + "'");
                Type indexType = node.Arguments.First().Type;
                if(indexType != typeof(int))
                    throw new InvalidOperationException("Indexing array with an index of type '" + indexType + "' is not allowed");
                context.EmitLoadArguments(node.Arguments.ToArray());
                context.Il.Ldloc(value);
                MethodInfo setMethod = arrayType.GetMethod("Set");
                if(setMethod == null)
                    throw new MissingMethodException(arrayType.ToString(), "Set");
                context.Il.Call(setMethod);
            }
        }

        private static void EmitIndexAccess(IndexExpression node, EmittingContext context)
        {
            if(node.Indexer != null)
            {
                context.EmitLoadArguments(node.Arguments.ToArray());
                MethodInfo getter = node.Indexer.GetGetMethod(true);
                if(getter == null)
                    throw new MissingMethodException(node.Indexer.ReflectedType.ToString(), "get_" + node.Indexer.Name);
                context.Il.Call(getter);
            }
            else
            {
                Type arrayType = node.Object.Type;
                if(!arrayType.IsArray)
                    throw new InvalidOperationException("An array expected");
                int rank = arrayType.GetArrayRank();
                if(rank != node.Arguments.Count)
                    throw new InvalidOperationException("Incorrect number of indeces '" + node.Arguments.Count + "' provided to access an array with rank '" + rank + "'");
                Type indexType = node.Arguments.First().Type;
                if(indexType != typeof(int))
                    throw new InvalidOperationException("Indexing array with an index of type '" + indexType + "' is not allowed");
                context.EmitLoadArguments(node.Arguments.ToArray());
                MethodInfo getMethod = arrayType.GetMethod("Get");
                if(getMethod == null)
                    throw new MissingMethodException(arrayType.ToString(), "Get");
                context.Il.Call(getMethod);
            }
        }
    }
}