using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class UnaryAssignExpressionEmitter : ExpressionEmitter<UnaryExpression>
    {
        protected override bool Emit(UnaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            GroboIL il = context.Il;
            var result = false;
            var operand = node.Operand;
            Type assigneeType;
            AssigneeKind assigneeKind;
            bool checkNullReferences = context.Options.HasFlag(CompilerOptions.CheckNullReferences);
            extend |= context.Options.HasFlag(CompilerOptions.ExtendOnAssign);
            switch(operand.NodeType)
            {
            case ExpressionType.Parameter:
                ExpressionEmittersCollection.Emit(operand, context, null, ResultType.ByRefAll, extend, out assigneeType);
                assigneeKind = AssigneeKind.Parameter;
                break;
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)operand;
                if(memberExpression.Expression == null)
                {
                    assigneeType = null;
                    assigneeKind = memberExpression.Member is FieldInfo ? AssigneeKind.StaticField : AssigneeKind.StaticProperty;
                }
                else
                {
                    bool closureAssign = memberExpression.Expression == context.ClosureParameter;
                    checkNullReferences &= !closureAssign;
                    var assigneeIsNullLabel = !closureAssign && context.CanReturn ? il.DefineLabel("assigneeIsNull") : null;
                    bool assigneeIsNullLabelUsed = ExpressionEmittersCollection.Emit(memberExpression.Expression, context, assigneeIsNullLabel, ResultType.ByRefValueTypesOnly, extend, out assigneeType);
                    if(assigneeType.IsValueType)
                    {
                        using(var temp = context.DeclareLocal(assigneeType))
                        {
                            il.Stloc(temp);
                            il.Ldloca(temp);
                        }
                        assigneeType = assigneeType.MakeByRefType();
                    }
                    if(assigneeIsNullLabelUsed)
                        context.EmitReturnDefaultValue(assigneeType, assigneeIsNullLabel, il.DefineLabel("assigneeIsNotNull"));
                    assigneeKind = memberExpression.Member is FieldInfo ? AssigneeKind.InstanceField : AssigneeKind.InstanceProperty;
                }
                break;
            case ExpressionType.Index:
                var indexExpression = (IndexExpression)operand;
                if(indexExpression.Object == null)
                    throw new InvalidOperationException("Indexing of null object is invalid");
                if(indexExpression.Object.Type.IsArray && indexExpression.Object.Type.GetArrayRank() == 1)
                {
                    var assigneeIsNullLabel = context.CanReturn ? il.DefineLabel("assigneeIsNull") : null;
                    bool assigneeIsNullLabelUsed = ExpressionEmittersCollection.Emit(Expression.ArrayIndex(indexExpression.Object, indexExpression.Arguments.Single()), context, assigneeIsNullLabel, ResultType.ByRefAll, extend, out assigneeType);
                    if(assigneeIsNullLabelUsed)
                        context.EmitReturnDefaultValue(assigneeType, assigneeIsNullLabel, il.DefineLabel("assigneeIsNotNull"));
                    assigneeKind = AssigneeKind.SimpleArray;
                }
                else
                {
                    var assigneeIsNullLabel = context.CanReturn ? il.DefineLabel("assigneeIsNull") : null;
                    bool assigneeIsNullLabelUsed = ExpressionEmittersCollection.Emit(indexExpression.Object, context, assigneeIsNullLabel, ResultType.ByRefValueTypesOnly, extend, out assigneeType);
                    if(assigneeType.IsValueType)
                    {
                        using(var temp = context.DeclareLocal(assigneeType))
                        {
                            il.Stloc(temp);
                            il.Ldloca(temp);
                        }
                        assigneeType = assigneeType.MakeByRefType();
                    }
                    if(assigneeIsNullLabelUsed)
                        context.EmitReturnDefaultValue(assigneeType, assigneeIsNullLabel, il.DefineLabel("assigneeIsNotNull"));
                    assigneeKind = indexExpression.Indexer != null ? AssigneeKind.IndexedProperty : AssigneeKind.MultiDimensionalArray;
                }
                break;
            default:
                throw new InvalidOperationException("Unable to assign to an expression of type '" + operand.NodeType + "'");
            }
            if(checkNullReferences && assigneeType != null)
            {
                result = true;
                il.Dup();
                il.Brfalse(returnDefaultValueLabel);
            }
            var assignee = assigneeType == null ? null : context.DeclareLocal(assigneeType);
            if(assignee != null)
            {
                il.Dup();
                il.Stloc(assignee);
            }
            switch(assigneeKind)
            {
            case AssigneeKind.Parameter:
            case AssigneeKind.SimpleArray:
                il.Ldind(operand.Type);
                break;
            case AssigneeKind.InstanceField:
            case AssigneeKind.StaticField:
                il.Ldfld((FieldInfo)((MemberExpression)operand).Member);
                break;
            case AssigneeKind.InstanceProperty:
            case AssigneeKind.StaticProperty:
                il.Call(((PropertyInfo)((MemberExpression)operand).Member).GetGetMethod(true));
                break;
            case AssigneeKind.IndexedProperty:
                {
                    var indexExpression = (IndexExpression)operand;
                    context.EmitLoadArguments(indexExpression.Arguments.ToArray());
                    MethodInfo getter = indexExpression.Indexer.GetGetMethod(true);
                    if(getter == null)
                        throw new MissingMethodException(indexExpression.Indexer.ReflectedType.ToString(), "get_" + indexExpression.Indexer.Name);
                    context.Il.Call(getter);
                }
                break;
            case AssigneeKind.MultiDimensionalArray:
                {
                    var indexExpression = (IndexExpression)operand;
                    Type arrayType = indexExpression.Object.Type;
                    if(!arrayType.IsArray)
                        throw new InvalidOperationException("An array expected");
                    int rank = arrayType.GetArrayRank();
                    if(rank != indexExpression.Arguments.Count)
                        throw new InvalidOperationException("Incorrect number of indeces '" + indexExpression.Arguments.Count + "' provided to access an array with rank '" + rank + "'");
                    Type indexType = indexExpression.Arguments.First().Type;
                    if(indexType != typeof(int))
                        throw new InvalidOperationException("Indexing array with an index of type '" + indexType + "' is not allowed");
                    context.EmitLoadArguments(indexExpression.Arguments.ToArray());
                    MethodInfo getMethod = arrayType.GetMethod("Get");
                    if(getMethod == null)
                        throw new MissingMethodException(arrayType.ToString(), "Get");
                    context.Il.Call(getMethod);
                }
                break;
            }
            var returnNullLabel = il.DefineLabel("returnNull");
            bool returnNullLabelUsed = false;
            var assignmentResult = context.DeclareLocal(operand.Type);
            il.Stloc(assignmentResult);
            if(!operand.Type.IsNullable())
            {
                il.Ldloc(assignmentResult);
                switch(node.NodeType)
                {
                case ExpressionType.PostIncrementAssign:
                    if(node.Method != null)
                        il.Call(node.Method);
                    else
                    {
                        il.Ldc_I4(1);
                        context.EmitConvert(typeof(int), operand.Type);
                        il.Add();
                    }
                    break;
                case ExpressionType.PostDecrementAssign:
                    if(node.Method != null)
                        il.Call(node.Method);
                    else
                    {
                        il.Ldc_I4(1);
                        context.EmitConvert(typeof(int), operand.Type);
                        il.Sub();
                    }
                    break;
                case ExpressionType.PreIncrementAssign:
                    if(node.Method != null)
                        il.Call(node.Method);
                    else
                    {
                        il.Ldc_I4(1);
                        context.EmitConvert(typeof(int), operand.Type);
                        il.Add();
                    }
                    il.Stloc(assignmentResult);
                    il.Ldloc(assignmentResult);
                    break;
                case ExpressionType.PreDecrementAssign:
                    if(node.Method != null)
                        il.Call(node.Method);
                    else
                    {
                        il.Ldc_I4(1);
                        context.EmitConvert(typeof(int), operand.Type);
                        il.Sub();
                    }
                    il.Stloc(assignmentResult);
                    il.Ldloc(assignmentResult);
                    break;
                }
            }
            else
            {
                il.Ldloca(assignmentResult);
                il.Dup();
                il.Ldfld(operand.Type.GetField("hasValue", BindingFlags.Instance | BindingFlags.NonPublic));
                if (checkNullReferences)
                {
                    result = true;
                    il.Brfalse(returnDefaultValueLabel);
                }
                else
                {
                    returnNullLabelUsed = true;
                    il.Brfalse(returnNullLabel);
                }
                il.Ldfld(operand.Type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic));
                Type argumentType = operand.Type.GetGenericArguments()[0];
                ConstructorInfo constructor = operand.Type.GetConstructor(new[] {argumentType});
                switch(node.NodeType)
                {
                case ExpressionType.PostIncrementAssign:
                    if(node.Method != null)
                        il.Call(node.Method);
                    else
                    {
                        il.Ldc_I4(1);
                        context.EmitConvert(typeof(int), argumentType);
                        il.Add();
                    }
                    il.Newobj(constructor);
                    break;
                case ExpressionType.PostDecrementAssign:
                    if(node.Method != null)
                        il.Call(node.Method);
                    else
                    {
                        il.Ldc_I4(1);
                        context.EmitConvert(typeof(int), argumentType);
                        il.Sub();
                    }
                    il.Newobj(constructor);
                    break;
                case ExpressionType.PreIncrementAssign:
                    if(node.Method != null)
                        il.Call(node.Method);
                    else
                    {
                        il.Ldc_I4(1);
                        context.EmitConvert(typeof(int), argumentType);
                        il.Add();
                    }
                    il.Newobj(constructor);
                    il.Stloc(assignmentResult);
                    il.Ldloc(assignmentResult);
                    break;
                case ExpressionType.PreDecrementAssign:
                    if(node.Method != null)
                        il.Call(node.Method);
                    else
                    {
                        il.Ldc_I4(1);
                        context.EmitConvert(typeof(int), argumentType);
                        il.Sub();
                    }
                    il.Newobj(constructor);
                    il.Stloc(assignmentResult);
                    il.Ldloc(assignmentResult);
                    break;
                }
            }
            using(var temp = context.DeclareLocal(operand.Type))
            {
                il.Stloc(temp);
                if(assignee != null)
                    il.Ldloc(assignee);
                switch(assigneeKind)
                {
                case AssigneeKind.Parameter:
                case AssigneeKind.SimpleArray:
                    il.Ldloc(temp);
                    il.Stind(operand.Type);
                    break;
                case AssigneeKind.InstanceField:
                case AssigneeKind.StaticField:
                    il.Ldloc(temp);
                    il.Stfld((FieldInfo)((MemberExpression)operand).Member);
                    break;
                case AssigneeKind.InstanceProperty:
                case AssigneeKind.StaticProperty:
                    il.Ldloc(temp);
                    il.Call(((PropertyInfo)((MemberExpression)operand).Member).GetSetMethod(true));
                    break;
                case AssigneeKind.IndexedProperty:
                    {
                        var indexExpression = (IndexExpression)operand;
                        context.EmitLoadArguments(indexExpression.Arguments.ToArray());
                        il.Ldloc(temp);
                        MethodInfo setter = indexExpression.Indexer.GetSetMethod(true);
                        if(setter == null)
                            throw new MissingMethodException(indexExpression.Indexer.ReflectedType.ToString(), "set_" + indexExpression.Indexer.Name);
                        context.Il.Call(setter);
                    }
                    break;
                case AssigneeKind.MultiDimensionalArray:
                    {
                        var indexExpression = (IndexExpression)operand;
                        Type arrayType = indexExpression.Object.Type;
                        if(!arrayType.IsArray)
                            throw new InvalidOperationException("An array expected");
                        int rank = arrayType.GetArrayRank();
                        if(rank != indexExpression.Arguments.Count)
                            throw new InvalidOperationException("Incorrect number of indeces '" + indexExpression.Arguments.Count + "' provided to access an array with rank '" + rank + "'");
                        Type indexType = indexExpression.Arguments.First().Type;
                        if(indexType != typeof(int))
                            throw new InvalidOperationException("Indexing array with an index of type '" + indexType + "' is not allowed");
                        context.EmitLoadArguments(indexExpression.Arguments.ToArray());
                        il.Ldloc(temp);
                        MethodInfo setMethod = arrayType.GetMethod("Set");
                        if(setMethod == null)
                            throw new MissingMethodException(arrayType.ToString(), "Set");
                        context.Il.Call(setMethod);
                    }
                    break;
                }
            }
            if(returnNullLabelUsed)
            {
                var doneLabel = il.DefineLabel("done");
                il.Br(doneLabel);
                il.MarkLabel(returnNullLabel);
                il.Pop();
                il.Ldloca(assignmentResult);
                il.Initobj(operand.Type);
                il.MarkLabel(doneLabel);
            }
            il.Ldloc(assignmentResult);
            if(assignee != null)
                assignee.Dispose();
            assignmentResult.Dispose();
            resultType = operand.Type;
            return result;
        }

        private enum AssigneeKind
        {
            Parameter,
            InstanceField,
            InstanceProperty,
            StaticField,
            StaticProperty,
            SimpleArray,
            MultiDimensionalArray,
            IndexedProperty
        }
    }
}