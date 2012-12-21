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
            var result = false;
            GroboIL il = context.Il;
            Type arrayType;
            result |= ExpressionEmittersCollection.Emit(node.Left, context, returnDefaultValueLabel, ResultType.Value, extend, out arrayType); // stack: [array]
            if(!arrayType.IsArray)
                throw new InvalidOperationException("Unable to perform array index operator to type '" + arrayType + "'");
            if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
            {
                result = true;
                il.Dup(); // stack: [array, array]
                il.Brfalse(returnDefaultValueLabel); // if(array == null) goto returnDefaultValue; stack: [array]
            }
            GroboIL.Label indexIsNullLabel = context.CanReturn ? il.DefineLabel("indexIsNull") : null;
            Type indexType;
            bool labelUsed = ExpressionEmittersCollection.Emit(node.Right, context, indexIsNullLabel, out indexType); // stack: [array, index]
            if(!indexType.IsPrimitive)
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
            using(var arrayIndex = context.DeclareLocal(typeof(int)))
            {
                il.Stloc(arrayIndex); // arrayIndex = index; stack: [array]
                if(extend && CanAssign(node.Left))
                {
                    result = true;
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
                        il.Stloc(array);
                        il.Ldloca(array);
                        il.Ldloc(arrayIndex);
                        il.Ldc_I4(1);
                        il.Add();
                        il.Call(arrayResizeMethod.MakeGenericMethod(arrayType.GetElementType()));
                        switch(node.Left.NodeType)
                        {
                        case ExpressionType.Parameter:
                            Type parameterType;
                            ExpressionEmittersCollection.Emit(node.Left, context, returnDefaultValueLabel, ResultType.ByRefAll, false, out parameterType);
                            il.Ldloc(array);
                            il.Stind(arrayType);
                            break;
                        case ExpressionType.MemberAccess:
                            var memberExpression = (MemberExpression)node.Left;
                            if(memberExpression.Expression == null)
                            {
                                if(memberExpression.Member.MemberType == MemberTypes.Field)
                                    il.Ldnull();
                            }
                            else
                            {
                                Type expressionType;
                                ExpressionEmittersCollection.Emit(memberExpression.Expression, context, returnDefaultValueLabel, ResultType.ByRefValueTypesOnly, false, out expressionType);
                                if(expressionType.IsValueType)
                                {
                                    using(var temp = context.DeclareLocal(expressionType))
                                    {
                                        il.Stloc(temp);
                                        il.Ldloca(temp);
                                    }
                                }
                            }
                            il.Ldloc(array);
                            switch(memberExpression.Member.MemberType)
                            {
                            case MemberTypes.Field:
                                il.Stfld((FieldInfo)memberExpression.Member);
                                break;
                            case MemberTypes.Property:
                                var propertyInfo = (PropertyInfo)memberExpression.Member;
                                var setter = propertyInfo.GetSetMethod(true);
                                if(setter == null)
                                    throw new MissingMethodException(propertyInfo.ReflectedType.ToString(), "set_"+propertyInfo.Name);
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
                    }
                    il.MarkLabel(bigEnoughLabel);
                }
                else if(context.Options.HasFlag(CompilerOptions.CheckArrayIndexes))
                {
                    result = true;
                    il.Dup(); // stack: [array, array]
                    il.Ldlen(); // stack: [array, array.Length]
                    il.Ldloc(arrayIndex); // stack: [array, array.Length, arrayIndex]
                    il.Ble(typeof(int), returnDefaultValueLabel); // if(array.Length <= arrayIndex) goto returnDefaultValue; stack: [array]
                    il.Ldloc(arrayIndex); // stack: [array, arrayIndex]
                    il.Ldc_I4(0); // stack: [array, arrayIndex, 0]
                    il.Blt(typeof(int), returnDefaultValueLabel); // if(arrayIndex < 0) goto returnDefaultValue; stack: [array]
                }
                if(extend && node.Type.IsClass)
                {
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
                il.Ldloc(arrayIndex);
            }
            switch(whatReturn)
            {
            case ResultType.Value:
                il.Ldelem(node.Type); // stack: [array[arrayIndex]]
                resultType = node.Type;
                break;
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
                throw new NotSupportedException("Result type '" + whatReturn + "' is not supported");
            }
            return result;
        }

        private static bool CanAssign(MemberInfo member)
        {
            return member.MemberType == MemberTypes.Field || (member.MemberType == MemberTypes.Property && ((PropertyInfo)member).CanWrite);
        }

        private static bool CanAssign(Expression node)
        {
            return node.NodeType == ExpressionType.Parameter || (node.NodeType == ExpressionType.MemberAccess && CanAssign(((MemberExpression)node).Member));
        }

        private static readonly MethodInfo arrayResizeMethod = ((MethodCallExpression)((Expression<Action<int[], int>>)((ints, len) => Array.Resize(ref ints, len))).Body).Method.GetGenericMethodDefinition();
    }
}