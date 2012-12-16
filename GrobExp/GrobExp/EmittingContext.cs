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
                EmitMemberAccess(type, type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance), false, out memberType);
                Il.Brfalse(objIsNullLabel);
                return true;
            }
            return false;
        }

        public void EmitMemberAccess(Type type, MemberInfo member, bool returnByRef, out Type memberType)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Property:
                var property = (PropertyInfo)member;
                var getter = property.GetGetMethod();
                if(getter == null)
                    throw new MissingMemberException(member.DeclaringType.Name, member.Name + "_get");
                Il.Call(getter, type);
                Type propertyType = property.PropertyType;
                if(returnByRef && propertyType.IsValueType)
                {
                    using(var temp = DeclareLocal(propertyType))
                    {
                        Il.Stloc(temp);
                        Il.Ldloca(temp);
                        memberType = propertyType.MakeByRefType();
                    }
                }
                else
                    memberType = propertyType;
                break;
            case MemberTypes.Field:
                var field = (FieldInfo)member;
                if(returnByRef && field.FieldType.IsValueType)
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
                throw new InvalidOperationException(); // todo exception
            }
        }

        public void EmitMemberAssign(Type type, MemberInfo member)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Property:
                var setter = ((PropertyInfo)member).GetSetMethod();
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

        private readonly Dictionary<ParameterExpression, LocalHolder> variablesToLocals = new Dictionary<ParameterExpression, LocalHolder>();
        private readonly Stack<ParameterExpression> variables = new Stack<ParameterExpression>();
        private readonly Dictionary<LabelTarget, GroboIL.Label> labels = new Dictionary<LabelTarget, GroboIL.Label>();

        private readonly Dictionary<Type, Queue<GroboIL.Local>> locals = new Dictionary<Type, Queue<GroboIL.Local>>();

        private static readonly FieldInfo nullableBoolValueField = typeof(bool?).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo nullableBoolHasValueField = typeof(bool?).GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}