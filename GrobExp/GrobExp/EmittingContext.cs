using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

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
                Type memberType;
                EmitMemberAccess(type, type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance), ResultType.Value, out memberType);
                Il.Brfalse(objIsNullLabel);
                return true;
            }
            return false;
        }

        public bool EmitMemberAccess(MemberExpression node, GroboIL.Label returnDefaultValueLabel, bool checkNullReferences, bool extend, ResultType whatReturn, out Type resultType, out LocalHolder owner)
        {
            bool result = false;
            owner = null;
            Type type = node.Expression == null ? null : node.Expression.Type;
            Type ownerType;
            GroboIL il = Il;
            if(node.Expression == null)
            {
                ownerType = null;
                if(node.Member.MemberType == MemberTypes.Field)
                    il.Ldnull();
            }
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
                    if(node.Member is FieldInfo)
                        il.Ldnull();
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
                var getter = property.GetGetMethod(true);
                if(getter == null)
                    throw new MissingMemberException(member.DeclaringType.Name, member.Name + "_get");
                Il.Call(getter, type);
                Type propertyType = property.PropertyType;
                switch(whatReturn)
                {
                case ResultType.Value:
                    memberType = propertyType;
                    break;
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
                    throw new NotSupportedException("Result type '" + whatReturn + "' is not supported");
                }
                break;
            case MemberTypes.Field:
                var field = (FieldInfo)member;
                switch(whatReturn)
                {
                case ResultType.Value:
                    Il.Ldfld(field);
                    memberType = field.FieldType;
                    break;
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
                    throw new NotSupportedException("Return type '" + whatReturn + "' is not supported");
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
                var setter = ((PropertyInfo)member).GetSetMethod(true);
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
                Il.Ldnull();
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

        public void EmitConvert(Type from, Type to)
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
                                FieldInfo hasValueField = from.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                                Il.Ldfld(hasValueField);
                                var valueIsNullLabel = Il.DefineLabel("valueIsNull");
                                Il.Brfalse(valueIsNullLabel);
                                Il.Ldloca(temp);
                                FieldInfo valueField = from.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                                Il.Ldfld(valueField);
                                if(toArgument != fromArgument)
                                    EmitConvert(fromArgument, toArgument);
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
                                EmitConvert(from, toArgument);
                            Il.Newobj(to.GetConstructor(new[] {toArgument}));
                        }
                    }
                    else if(to.IsEnum || to == typeof(Enum))
                        EmitConvert(from, typeof(int));
                    else if(from.IsEnum || from == typeof(Enum))
                        EmitConvert(typeof(int), to);
                    else
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
                        EmitConvertValue(Il, fromTypeCode, toTypeCode);
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
                Il.Ldfld(nullableBoolValueField);
            }
        }

        public CompilerOptions Options { get; set; }
        public ParameterExpression[] Parameters { get; set; }
        public Type ClosureType { get; set; }
        public ParameterExpression ClosureParameter { get; set; }
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

        private static void EmitConvertValue(GroboIL il, TypeCode fromTypeCode, TypeCode toTypeCode)
        {
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

        private static readonly FieldInfo nullableBoolValueField = typeof(bool?).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo nullableBoolHasValueField = typeof(bool?).GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}