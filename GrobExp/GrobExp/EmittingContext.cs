using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrEmit;

namespace GrobExp
{
    internal class EmittingContext
    {
        public bool EmitNullChecking(Type type, GroboIL.Label objIsNullLabel)
        {
            if(!type.IsValueType)
            {
                Il.Dup(); // stack: [obj, obj]
                Il.Brfalse(objIsNullLabel); // if(obj == null) goto returnDefaultValue; stack: [obj]
                return true;
            }
            if(type.IsNullable())
            {
                Il.Dup();
                EmitHasValueAccess(type);
                Il.Brfalse(objIsNullLabel);
                return true;
            }
            return false;
        }

        public void EmitHasValueAccess(Type type)
        {
            Type memberType;
            MemberInfo member = SkipVisibility ? (MemberInfo)type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance) : type.GetProperty("HasValue", BindingFlags.Public | BindingFlags.Instance);
            EmitMemberAccess(type, member, ResultType.Value, out memberType);
        }

        public void EmitValueAccess(Type type)
        {
            Type memberType;
            if(SkipVisibility)
                EmitMemberAccess(type, type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance), ResultType.Value, out memberType);
            else
                Il.Call(type.GetMethod("GetValueOrDefault", Type.EmptyTypes));
        }

        public bool EmitMemberAccess(MemberExpression node, GroboIL.Label returnDefaultValueLabel, bool checkNullReferences, bool extend, ResultType whatReturn, out Type resultType, out LocalHolder owner)
        {
            bool result = false;
            owner = null;
            Type type = node.Expression == null ? null : node.Expression.Type;
            Type ownerType;
            GroboIL il = Il;
            if(node.Expression == null)
                ownerType = null;
            else
            {
                result |= ExpressionEmittersCollection.Emit(node.Expression, this, returnDefaultValueLabel, ResultType.ByRefValueTypesOnly, extend, out type); // stack: [obj]
                if(!type.IsValueType)
                    ownerType = type;
                else
                {
                    ownerType = type.MakeByRefType();
                    using(var temp = DeclareLocal(type))
                    {
                        il.Stloc(temp);
                        il.Ldloca(temp);
                    }
                }
                if(checkNullReferences && node.Expression != ClosureParameter)
                    result |= EmitNullChecking(node.Expression.Type, returnDefaultValueLabel);
            }
            extend &= CanAssign(node.Member);
            Type memberType = GetMemberType(node.Member);
            ConstructorInfo constructor = memberType.GetConstructor(Type.EmptyTypes);
            extend &= (memberType.IsClass && constructor != null) || memberType.IsArray;
            if(!extend)
                EmitMemberAccess(type, node.Member, whatReturn, out resultType); // stack: [obj.member]
            else
            {
                if(node.Expression == null)
                {
                    EmitMemberAccess(type, node.Member, whatReturn, out resultType); // stack: [obj.member]
                    var memberIsNotNullLabel = il.DefineLabel("memberIsNotNull");
                    il.Dup();
                    il.Brtrue(memberIsNotNullLabel);
                    il.Pop();
                    if(!memberType.IsArray)
                        il.Newobj(constructor);
                    else
                    {
                        il.Ldc_I4(0);
                        il.Newarr(memberType.GetElementType());
                    }
                    using(var newobj = DeclareLocal(memberType))
                    {
                        il.Stloc(newobj);
                        il.Ldloc(newobj);
                        EmitMemberAssign(type, node.Member);
                        il.Ldloc(newobj);
                    }
                    il.MarkLabel(memberIsNotNullLabel);
                }
                else
                {
                    owner = DeclareLocal(ownerType);
                    il.Stloc(owner);
                    il.Ldloc(owner);
                    EmitMemberAccess(type, node.Member, whatReturn, out resultType); // stack: [obj.member]
                    var memberIsNotNullLabel = il.DefineLabel("memberIsNotNull");
                    il.Dup();
                    il.Brtrue(memberIsNotNullLabel);
                    il.Pop();
                    il.Ldloc(owner);
                    if(!memberType.IsArray)
                        il.Newobj(constructor);
                    else
                    {
                        il.Ldc_I4(0);
                        il.Newarr(memberType.GetElementType());
                    }
                    using(var newobj = DeclareLocal(memberType))
                    {
                        il.Stloc(newobj);
                        il.Ldloc(newobj);
                        EmitMemberAssign(type, node.Member);
                        il.Ldloc(newobj);
                    }
                    il.MarkLabel(memberIsNotNullLabel);
                }
            }
            return result;
        }

        public void EmitMemberAccess(Type type, MemberInfo member, ResultType whatReturn, out Type memberType)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Property:
                var property = (PropertyInfo)member;
                var getter = property.GetGetMethod(SkipVisibility);
                if(getter == null)
                    throw new MissingMemberException(member.DeclaringType.Name, member.Name + "_get");
                Il.Call(getter, type);
                Type propertyType = property.PropertyType;
                switch(whatReturn)
                {
                case ResultType.ByRefValueTypesOnly:
                    if(!propertyType.IsValueType)
                        memberType = propertyType;
                    else
                    {
                        using(var temp = DeclareLocal(propertyType))
                        {
                            Il.Stloc(temp);
                            Il.Ldloca(temp);
                            memberType = propertyType.MakeByRefType();
                        }
                    }
                    break;
                case ResultType.ByRefAll:
                    throw new InvalidOperationException("It's wierd to load a property by ref for a reference type");
                default:
                    memberType = propertyType;
                    break;
                }
                break;
            case MemberTypes.Field:
                var field = (FieldInfo)member;
                switch(whatReturn)
                {
                case ResultType.ByRefAll:
                    Il.Ldflda(field);
                    memberType = field.FieldType.MakeByRefType();
                    break;
                case ResultType.ByRefValueTypesOnly:
                    if(field.FieldType.IsValueType)
                    {
                        Il.Ldflda(field);
                        memberType = field.FieldType.MakeByRefType();
                    }
                    else
                    {
                        Il.Ldfld(field);
                        memberType = field.FieldType;
                    }
                    break;
                default:
                    Il.Ldfld(field);
                    memberType = field.FieldType;
                    break;
                }
                break;
            default:
                throw new NotSupportedException("Member type '" + member.MemberType + "' is not supported");
            }
        }

        public void EmitMemberAssign(Type type, MemberInfo member)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Property:
                var setter = ((PropertyInfo)member).GetSetMethod(SkipVisibility);
                if(setter == null)
                    throw new MissingMemberException(member.DeclaringType.Name, member.Name + "_set");
                Il.Call(setter, type);
                break;
            case MemberTypes.Field:
                Il.Stfld((FieldInfo)member);
                break;
            }
        }

        public void EmitLoadArguments(params Expression[] arguments)
        {
            foreach(var argument in arguments)
            {
                Type argumentType;
                EmitLoadArgument(argument, true, out argumentType);
            }
        }

        public void EmitLoadArgument(Expression argument, bool convertToBool, out Type argumentType)
        {
            var argumentIsNullLabel = CanReturn ? Il.DefineLabel("argumentIsNull") : null;
            bool labelUsed = ExpressionEmittersCollection.Emit(argument, this, argumentIsNullLabel, out argumentType);
            if(convertToBool && argument.Type == typeof(bool) && argumentType == typeof(bool?))
            {
                ConvertFromNullableBoolToBool();
                argumentType = typeof(bool);
            }
            if(labelUsed)
                EmitReturnDefaultValue(argument.Type, argumentIsNullLabel, Il.DefineLabel("argumentIsNotNull"));
        }

        public void EmitLoadDefaultValue(Type type)
        {
            if(!type.IsValueType)
                Il.Ldnull(type);
            else
            {
                using(var temp = DeclareLocal(type))
                {
                    Il.Ldloca(temp);
                    Il.Initobj(type);
                    Il.Ldloc(temp);
                }
            }
        }

        public void EmitReturnDefaultValue(Type type, GroboIL.Label valueIsNullLabel, GroboIL.Label valueIsNotNullLabel)
        {
            Il.Br(valueIsNotNullLabel);
            Il.MarkLabel(valueIsNullLabel);
            Il.Pop();
            EmitLoadDefaultValue(type);
            Il.MarkLabel(valueIsNotNullLabel);
        }

        public void EmitArithmeticOperation(ExpressionType nodeType, Type resultType, Type leftType, Type rightType, MethodInfo method)
        {
            if(!leftType.IsNullable() && !rightType.IsNullable())
            {
                if(method != null)
                    Il.Call(method);
                else
                {
                    if(leftType.IsStruct())
                        throw new InvalidOperationException("Unable to perfrom operation '" + nodeType + "' to a struct of type '" + leftType + "'");
                    if(rightType.IsStruct())
                        throw new InvalidOperationException("Unable to perfrom operation '" + nodeType + "' to a struct of type '" + rightType + "'");
                    EmitOp(Il, nodeType, resultType);
                }
            }
            else
            {
                using(var localLeft = DeclareLocal(leftType))
                using(var localRight = DeclareLocal(rightType))
                {
                    Il.Stloc(localRight);
                    Il.Stloc(localLeft);
                    var returnNullLabel = Il.DefineLabel("returnNull");
                    if(leftType.IsNullable())
                    {
                        Il.Ldloca(localLeft);
                        EmitHasValueAccess(leftType);
                        Il.Brfalse(returnNullLabel);
                    }
                    if(rightType.IsNullable())
                    {
                        Il.Ldloca(localRight);
                        EmitHasValueAccess(rightType);
                        Il.Brfalse(returnNullLabel);
                    }
                    if(!leftType.IsNullable())
                        Il.Ldloc(localLeft);
                    else
                    {
                        Il.Ldloca(localLeft);
                        EmitValueAccess(leftType);
                    }
                    if(!rightType.IsNullable())
                        Il.Ldloc(localRight);
                    else
                    {
                        Il.Ldloca(localRight);
                        EmitValueAccess(rightType);
                    }
                    Type argumentType = resultType.GetGenericArguments()[0];
                    if(method != null)
                        Il.Call(method);
                    else
                        EmitOp(Il, nodeType, argumentType);
                    Il.Newobj(resultType.GetConstructor(new[] {argumentType}));

                    var doneLabel = Il.DefineLabel("done");
                    Il.Br(doneLabel);
                    Il.MarkLabel(returnNullLabel);
                    EmitLoadDefaultValue(resultType);
                    Il.MarkLabel(doneLabel);
                }
            }
        }

        public void EmitConvert(Type from, Type to, bool check = false)
        {
            if(from == to) return;
            if(!from.IsValueType)
            {
                if(!to.IsValueType)
                    Il.Castclass(to);
                else
                {
                    if(from != typeof(object) && !(from == typeof(Enum) && to.IsEnum))
                        throw new InvalidCastException("Cannot cast an object of type '" + from + "' to type '" + to + "'");
                    Il.Unbox_Any(to);
                }
            }
            else
            {
                if(!to.IsValueType)
                {
                    if(to != typeof(object) && !(to == typeof(Enum) && from.IsEnum))
                        throw new InvalidCastException("Cannot cast an object of type '" + from + "' to type '" + to + "'");
                    Il.Box(from);
                }
                else
                {
                    if(to.IsNullable())
                    {
                        var toArgument = to.GetGenericArguments()[0];
                        if(from.IsNullable())
                        {
                            var fromArgument = from.GetGenericArguments()[0];
                            using(var temp = DeclareLocal(from))
                            {
                                Il.Stloc(temp);
                                Il.Ldloca(temp);
                                EmitHasValueAccess(from);
                                var valueIsNullLabel = Il.DefineLabel("valueIsNull");
                                Il.Brfalse(valueIsNullLabel);
                                Il.Ldloca(temp);
                                EmitValueAccess(from);
                                if(toArgument != fromArgument)
                                    EmitConvert(fromArgument, toArgument, check);
                                Il.Newobj(to.GetConstructor(new[] {toArgument}));
                                var doneLabel = Il.DefineLabel("done");
                                Il.Br(doneLabel);
                                Il.MarkLabel(valueIsNullLabel);
                                EmitLoadDefaultValue(to);
                                Il.MarkLabel(doneLabel);
                            }
                        }
                        else
                        {
                            if(toArgument != from)
                                EmitConvert(from, toArgument, check);
                            Il.Newobj(to.GetConstructor(new[] {toArgument}));
                        }
                    }
                    else if(to.IsEnum || to == typeof(Enum))
                        EmitConvert(from, typeof(int), check);
                    else if(from.IsEnum || from == typeof(Enum))
                        EmitConvert(typeof(int), to, check);
                    else
                    {
                        if(!check)
                            EmitConvertValue(Il, from, to);
                        else
                            EmitConvertValueChecked(Il, from, to);
                    }
                }
            }
        }

        public LocalHolder DeclareLocal(Type type)
        {
            Queue<GroboIL.Local> queue;
            if(!locals.TryGetValue(type, out queue))
            {
                queue = new Queue<GroboIL.Local>();
                locals.Add(type, queue);
            }
            if(queue.Count == 0)
                queue.Enqueue(Il.DeclareLocal(type));
            return new LocalHolder(this, type, queue.Dequeue());
        }

        public void FreeLocal(Type type, GroboIL.Local local)
        {
            locals[type].Enqueue(local);
        }

        public void ConvertFromNullableBoolToBool()
        {
            using(var temp = DeclareLocal(typeof(bool?)))
            {
                Il.Stloc(temp);
                Il.Ldloca(temp);
                EmitValueAccess(typeof(bool?));
            }
        }

        public CompilerOptions Options { get; set; }
        public TypeBuilder TypeBuilder { get; set; }
        public DebugInfoGenerator DebugInfoGenerator { get; set; }
        public bool SequencePointCleared { get; set; }
        public LambdaExpression Lambda { get; set; }
        public MethodInfo Method { get; set; }
        public bool SkipVisibility { get; set; }
        public ParameterExpression[] Parameters { get; set; }
        public Type ClosureType { get; set; }
        public ParameterExpression ClosureParameter { get; set; }
        public Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> Switches { get; set; }
        public List<CompiledLambda> CompiledLambdas { get; set; }
        public GroboIL Il { get; set; }
        public Dictionary<ParameterExpression, LocalHolder> VariablesToLocals { get { return variablesToLocals; } }
        public Dictionary<LabelTarget, GroboIL.Label> Labels { get { return labels; } }
        public Stack<ParameterExpression> Variables { get { return variables; } }

        public bool CanReturn { get { return Options.HasFlag(CompilerOptions.CheckNullReferences) || Options.HasFlag(CompilerOptions.CheckArrayIndexes); } }

        public class LocalHolder : IDisposable
        {
            public LocalHolder(EmittingContext owner, Type type, GroboIL.Local local)
            {
                this.owner = owner;
                this.type = type;
                this.local = local;
            }

            public void Dispose()
            {
                owner.FreeLocal(type, local);
            }

            public static implicit operator GroboIL.Local(LocalHolder holder)
            {
                return holder.local;
            }

            private readonly EmittingContext owner;
            private readonly Type type;
            private readonly GroboIL.Local local;
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
            case ExpressionType.And:
                il.And();
                break;
            case ExpressionType.Or:
                il.Or();
                break;
            case ExpressionType.ExclusiveOr:
                il.Xor();
                break;
            default:
                throw new NotSupportedException("Node type '" + nodeType + "' is not supported");
            }
        }

        private static void EmitConvertValue(GroboIL il, Type from, Type to)
        {
            var fromTypeCode = Type.GetTypeCode(from);
            var toTypeCode = Type.GetTypeCode(to);
            switch(fromTypeCode)
            {
            case TypeCode.DBNull:
            case TypeCode.DateTime:
            case TypeCode.Decimal:
            case TypeCode.Empty:
            case TypeCode.Object:
            case TypeCode.String:
                throw new NotSupportedException("Cast from type '" + from + "' to type '" + to + "' is not supported");
            }
            if(toTypeCode == fromTypeCode)
                return;
            switch(toTypeCode)
            {
            case TypeCode.SByte:
                il.Conv_I1();
                break;
            case TypeCode.Byte:
            case TypeCode.Boolean:
                il.Conv_U1();
                break;
            case TypeCode.Int16:
                il.Conv_I2();
                break;
            case TypeCode.UInt16:
                il.Conv_U2();
                break;
            case TypeCode.Int32:
                if(fromTypeCode == TypeCode.Int64 || fromTypeCode == TypeCode.UInt64 || fromTypeCode == TypeCode.Double || fromTypeCode == TypeCode.Single /* || fromTypeCode == TypeCode.DateTime*/)
                    il.Conv_I4();
                break;
            case TypeCode.UInt32:
                if(fromTypeCode == TypeCode.Int64 || fromTypeCode == TypeCode.UInt64 || fromTypeCode == TypeCode.Double || fromTypeCode == TypeCode.Single /* || fromTypeCode == TypeCode.DateTime*/)
                    il.Conv_U4();
                break;
            case TypeCode.Int64:
                if(fromTypeCode != TypeCode.UInt64)
                {
                    if(fromTypeCode == TypeCode.Byte || fromTypeCode == TypeCode.UInt16 || fromTypeCode == TypeCode.Char || fromTypeCode == TypeCode.UInt32)
                        il.Conv_U8();
                    else
                        il.Conv_I8();
                }
                break;
            case TypeCode.UInt64:
                if(fromTypeCode != TypeCode.Int64 /* && fromTypeCode != TypeCode.DateTime*/)
                {
                    if(fromTypeCode == TypeCode.SByte || fromTypeCode == TypeCode.Int16 || fromTypeCode == TypeCode.Int32)
                        il.Conv_I8();
                    else
                        il.Conv_U8();
                }
                break;
            case TypeCode.Single:
                if(fromTypeCode == TypeCode.UInt64 || fromTypeCode == TypeCode.UInt32)
                    il.Conv_R_Un();
                il.Conv_R4();
                break;
            case TypeCode.Double:
                if(fromTypeCode == TypeCode.UInt64 || fromTypeCode == TypeCode.UInt32)
                    il.Conv_R_Un();
                il.Conv_R8();
                break;
            default:
                throw new NotSupportedException("Type with type code '" + toTypeCode + "' is not supported");
            }
        }

        private static void EmitConvertValueChecked(GroboIL il, Type from, Type to)
        {
            var fromTypeCode = Type.GetTypeCode(from);
            var toTypeCode = Type.GetTypeCode(to);
            switch(fromTypeCode)
            {
            case TypeCode.DBNull:
            case TypeCode.DateTime:
            case TypeCode.Decimal:
            case TypeCode.Empty:
            case TypeCode.Object:
            case TypeCode.String:
                throw new NotSupportedException("Cast from type '" + from + "' to type '" + to + "' is not supported");
            }
            if(toTypeCode == fromTypeCode)
                return;
            switch(toTypeCode)
            {
            case TypeCode.SByte:
                il.Conv_Ovf_I1(from);
                break;
            case TypeCode.Byte:
            case TypeCode.Boolean:
                il.Conv_Ovf_U1(from);
                break;
            case TypeCode.Int16:
                il.Conv_Ovf_I2(from);
                break;
            case TypeCode.UInt16:
                il.Conv_Ovf_U2(from);
                break;
            case TypeCode.Int32:
                if(fromTypeCode == TypeCode.UInt32 || fromTypeCode == TypeCode.Int64 || fromTypeCode == TypeCode.UInt64
                   || fromTypeCode == TypeCode.Double || fromTypeCode == TypeCode.Single /* || fromTypeCode == TypeCode.DateTime*/)
                    il.Conv_Ovf_I4(from);
                break;
            case TypeCode.UInt32:
                if(fromTypeCode == TypeCode.SByte || fromTypeCode == TypeCode.Int16 || fromTypeCode == TypeCode.Int32 || fromTypeCode == TypeCode.Int64
                   || fromTypeCode == TypeCode.UInt64 || fromTypeCode == TypeCode.Double || fromTypeCode == TypeCode.Single /* || fromTypeCode == TypeCode.DateTime*/)
                    il.Conv_Ovf_U4(from);
                break;
            case TypeCode.Int64:
                switch(fromTypeCode)
                {
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.UInt64:
                    il.Conv_Ovf_I8(from);
                    break;
                case TypeCode.UInt32:
                case TypeCode.Char:
                case TypeCode.UInt16:
                case TypeCode.Byte:
                    il.Conv_U8();
                    break;
                default:
                    il.Conv_I8();
                    break;
                }
                break;
            case TypeCode.UInt64:
                switch(fromTypeCode)
                {
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                    il.Conv_Ovf_U8(from);
                    break;
                case TypeCode.UInt32:
                case TypeCode.Char:
                case TypeCode.UInt16:
                case TypeCode.Byte:
                    il.Conv_U8();
                    break;
                }
                break;
            case TypeCode.Single:
                if(fromTypeCode == TypeCode.UInt64 || fromTypeCode == TypeCode.UInt32)
                    il.Conv_R_Un();
                il.Conv_R4();
                break;
            case TypeCode.Double:
                if(fromTypeCode == TypeCode.UInt64 || fromTypeCode == TypeCode.UInt32)
                    il.Conv_R_Un();
                il.Conv_R8();
                break;
            default:
                throw new NotSupportedException("Type with type code '" + toTypeCode + "' is not supported");
            }
        }

        private static bool CanAssign(MemberInfo member)
        {
            return member.MemberType == MemberTypes.Field || (member.MemberType == MemberTypes.Property && ((PropertyInfo)member).CanWrite);
        }

        private static Type GetMemberType(MemberInfo member)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Field:
                return ((FieldInfo)member).FieldType;
            case MemberTypes.Property:
                return ((PropertyInfo)member).PropertyType;
            default:
                throw new NotSupportedException("Member " + member + " is not supported");
            }
        }

        private readonly Dictionary<ParameterExpression, LocalHolder> variablesToLocals = new Dictionary<ParameterExpression, LocalHolder>();
        private readonly Stack<ParameterExpression> variables = new Stack<ParameterExpression>();
        private readonly Dictionary<LabelTarget, GroboIL.Label> labels = new Dictionary<LabelTarget, GroboIL.Label>();

        private readonly Dictionary<Type, Queue<GroboIL.Local>> locals = new Dictionary<Type, Queue<GroboIL.Local>>();
    }
}