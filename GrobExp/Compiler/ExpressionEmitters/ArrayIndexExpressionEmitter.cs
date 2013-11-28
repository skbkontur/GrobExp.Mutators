using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ArrayIndexExpressionEmitter : ExpressionEmitter<BinaryExpression>
    {
        protected override bool Emit(BinaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var arrayType = node.Left.Type;
            if(!arrayType.IsArray)
                throw new InvalidOperationException("Unable to perform array index operator to type '" + arrayType + "'");
            GroboIL il = context.Il;
            EmittingContext.LocalHolder arrayIndex = null;
            bool extendArray = extend && CanAssign(node.Left);
            bool extendArrayElement = extend && arrayType.GetElementType().IsClass;
            var result = false;
            if(!extendArray)
            {
                result |= ExpressionEmittersCollection.Emit(node.Left, context, returnDefaultValueLabel, ResultType.Value, extend, out arrayType); // stack: [array]
                if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                {
                    result = true;
                    il.Dup(); // stack: [array, array]
                    il.Brfalse(returnDefaultValueLabel); // if(array == null) goto returnDefaultValue; stack: [array]
                }
                EmitLoadIndex(node.Right, context, arrayType); // stack: [array, arrayIndex]
                if(context.Options.HasFlag(CompilerOptions.CheckArrayIndexes))
                {
                    result = true;
                    arrayIndex = context.DeclareLocal(typeof(int));
                    il.Stloc(arrayIndex); // arrayIndex = index; stack: [array]
                    il.Dup(); // stack: [array, array]
                    il.Ldlen(); // stack: [array, array.Length]
                    il.Ldloc(arrayIndex); // stack: [array, array.Length, arrayIndex]
                    il.Ble(typeof(int), returnDefaultValueLabel); // if(array.Length <= arrayIndex) goto returnDefaultValue; stack: [array]
                    il.Ldloc(arrayIndex); // stack: [array, arrayIndex]
                    il.Ldc_I4(0); // stack: [array, arrayIndex, 0]
                    il.Blt(typeof(int), returnDefaultValueLabel); // if(arrayIndex < 0) goto returnDefaultValue; stack: [array]
                }
                else if(extendArrayElement)
                {
                    arrayIndex = context.DeclareLocal(typeof(int));
                    il.Stloc(arrayIndex); // arrayIndex = index; stack: [array]
                }
            }
            else
            {
                EmittingContext.LocalHolder arrayOwner = null;
                switch(node.Left.NodeType)
                {
                case ExpressionType.Parameter:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Index:
                    Type type;
                    ExpressionEmittersCollection.Emit(node.Left, context, returnDefaultValueLabel, ResultType.ByRefAll, true, out type); // stack: [ref array]
                    arrayOwner = context.DeclareLocal(type);
                    il.Dup(); // stack: [ref array, ref array]
                    il.Stloc(arrayOwner); // arrayOwner = ref array; stack: [ref array]
                    il.Ldind(node.Left.Type); // stack: [array]
                    break;
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)node.Left;
                    Type memberType;
                    context.EmitMemberAccess(memberExpression, returnDefaultValueLabel, context.Options.HasFlag(CompilerOptions.CheckNullReferences), true, ResultType.ByRefValueTypesOnly, out memberType, out arrayOwner); // stack: [array]
                    break;
                default:
                    throw new InvalidOperationException("Cannot extend array for expression with node type '" + node.Left.NodeType + "'");
                }
                if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                {
                    il.Dup(); // stack: [array, array]
                    il.Brfalse(returnDefaultValueLabel); // if(array == null) goto returnDefaultValue; stack: [array]
                }
                EmitLoadIndex(node.Right, context, arrayType);
                result = true;
                arrayIndex = context.DeclareLocal(typeof(int));
                il.Stloc(arrayIndex); // arrayIndex = index; stack: [array]
                il.Ldloc(arrayIndex); // stack: [array, arrayIndex]
                il.Ldc_I4(0); // stack: [array, arrayIndex, 0]
                il.Blt(typeof(int), returnDefaultValueLabel); // if(arrayIndex < 0) goto returnDefaultValue; stack: [array]
                il.Dup(); // stack: [array, array]
                il.Ldlen(); // stack: [array, array.Length]
                il.Ldloc(arrayIndex); // stack: [array, array.Length, arrayIndex]
                var bigEnoughLabel = il.DefineLabel("bigEnough");
                il.Bgt(typeof(int), bigEnoughLabel); // if(array.Length > arrayIndex) goto bigEnough; stack: [array]
                using(var array = context.DeclareLocal(arrayType))
                {
                    il.Stloc(array); // stack: []
                    il.Ldloca(array); // stack: [ref array]
                    il.Ldloc(arrayIndex); // stack: [ref array, arrayIndex]
                    il.Ldc_I4(1); // stack: [ref array, arrayIndex, 1]
                    il.Add(); // stack: [ref array, arrayIndex + 1]
                    il.Call(arrayResizeMethod.MakeGenericMethod(arrayType.GetElementType())); // Array.Resize(ref array, 1 + arrayIndex); stack: []
                    switch(node.Left.NodeType)
                    {
                    case ExpressionType.Parameter:
                    case ExpressionType.ArrayIndex:
                    case ExpressionType.Index:
                        il.Ldloc(arrayOwner); // stack: [ref parameter]
                        il.Ldloc(array); // stack: [ref parameter, array]
                        il.Stind(arrayType); // parameter = array; stack: []
                        break;
                    case ExpressionType.MemberAccess:
                        var memberExpression = (MemberExpression)node.Left;
                        if(memberExpression.Expression != null)
                            il.Ldloc(arrayOwner);
                        il.Ldloc(array);
                        switch(memberExpression.Member.MemberType)
                        {
                        case MemberTypes.Field:
                            il.Stfld((FieldInfo)memberExpression.Member);
                            break;
                        case MemberTypes.Property:
                            var propertyInfo = (PropertyInfo)memberExpression.Member;
                            var setter = propertyInfo.GetSetMethod(context.SkipVisibility);
                            if(setter == null)
                                throw new MissingMethodException(propertyInfo.ReflectedType.ToString(), "set_" + propertyInfo.Name);
                            il.Call(setter, memberExpression.Expression == null ? null : memberExpression.Expression.Type);
                            break;
                        default:
                            throw new NotSupportedException("Member type '" + memberExpression.Member.MemberType + "' is not supported");
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unable to assign array to an expression with node type '" + node.Left.NodeType);
                    }
                    il.Ldloc(array);
                    il.MarkLabel(bigEnoughLabel);
                }
            }

            if(extendArrayElement)
            {
                // stack: [array]
                var constructor = node.Type.GetConstructor(Type.EmptyTypes);
                if(node.Type.IsArray || constructor != null)
                {
                    using(var array = context.DeclareLocal(node.Left.Type))
                    {
                        il.Dup(); // stack: [array, array]
                        il.Stloc(array); // stack: [array]
                        il.Ldloc(arrayIndex); // stack: [array, arrayIndex]
                        il.Ldelem(node.Type); // stack: [array[arrayIndex]]
                        var elementIsNotNullLabel = il.DefineLabel("elementIsNotNull");
                        il.Brtrue(elementIsNotNullLabel);
                        il.Ldloc(array);
                        il.Ldloc(arrayIndex);
                        if(!node.Type.IsArray)
                            il.Newobj(constructor);
                        else
                        {
                            il.Ldc_I4(0);
                            il.Newarr(node.Type.GetElementType());
                        }
                        il.Stelem(node.Type);
                        il.MarkLabel(elementIsNotNullLabel);
                        il.Ldloc(array);
                    }
                }
            }
            if(arrayIndex != null)
            {
                il.Ldloc(arrayIndex);
                arrayIndex.Dispose();
            }
            switch(whatReturn)
            {
            case ResultType.ByRefAll:
                il.Ldelema(node.Type);
                resultType = node.Type.MakeByRefType();
                break;
            case ResultType.ByRefValueTypesOnly:
                if(node.Type.IsValueType)
                {
                    il.Ldelema(node.Type);
                    resultType = node.Type.MakeByRefType();
                }
                else
                {
                    il.Ldelem(node.Type); // stack: [array[arrayIndex]]
                    resultType = node.Type;
                }
                break;
            default:
                il.Ldelem(node.Type); // stack: [array[arrayIndex]]
                resultType = node.Type;
                break;
            }
            return result;
        }

        private static bool CanAssign(MemberInfo member)
        {
            return member.MemberType == MemberTypes.Field || (member.MemberType == MemberTypes.Property && ((PropertyInfo)member).CanWrite);
        }

        private static bool CanAssign(IndexExpression indexExpression)
        {
            return indexExpression.Object != null && indexExpression.Object.Type.IsArray && indexExpression.Object.Type.GetArrayRank() == 1;
        }

        private static bool CanAssign(Expression node)
        {
            return node.NodeType == ExpressionType.Parameter
                   || node.NodeType == ExpressionType.ArrayIndex
                   || (node.NodeType == ExpressionType.Index && CanAssign((IndexExpression)node))
                   || (node.NodeType == ExpressionType.MemberAccess && CanAssign(((MemberExpression)node).Member));
        }

        private static void EmitLoadIndex(Expression index, EmittingContext context, Type arrayType)
        {
            GroboIL il = context.Il;
            GroboIL.Label indexIsNullLabel = context.CanReturn ? il.DefineLabel("indexIsNull") : null;
            Type indexType;
            bool labelUsed = ExpressionEmittersCollection.Emit(index, context, indexIsNullLabel, out indexType); // stack: [array, index]
            if(indexType != typeof(int))
                throw new InvalidOperationException("Unable to perform array index operator to type '" + arrayType + "'");
            if(labelUsed && context.CanReturn)
            {
                var indexIsNotNullLabel = il.DefineLabel("indexIsNotNull");
                il.Br(indexIsNotNullLabel);
                il.MarkLabel(indexIsNullLabel);
                il.Pop();
                il.Ldc_I4(0);
                il.MarkLabel(indexIsNotNullLabel);
            }
        }

        private static readonly MethodInfo arrayResizeMethod = ((MethodCallExpression)((Expression<Action<int[], int>>)((ints, len) => Array.Resize(ref ints, len))).Body).Method.GetGenericMethodDefinition();
    }
}