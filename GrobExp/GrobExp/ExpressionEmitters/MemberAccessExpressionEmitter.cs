using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class MemberAccessExpressionEmitter : ExpressionEmitter<MemberExpression>
    {
        protected override bool Emit(MemberExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            bool result = false;
            Type type = node.Expression == null ? null : node.Expression.Type;
            GroboIL il = context.Il;
            if(node.Expression == null)
            {
                if(node.Member.MemberType == MemberTypes.Field)
                    il.Ldnull();
            }
            else
            {
                result |= ExpressionEmittersCollection.Emit(node.Expression, context, returnDefaultValueLabel, ResultType.ByRefValueTypesOnly, extend, out type); // stack: [obj]
                if(type.IsValueType)
                {
                    using(var temp = context.DeclareLocal(type))
                    {
                        il.Stloc(temp);
                        il.Ldloca(temp);
                    }
                }
                if(node.Expression != context.ClosureParameter && context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                    result |= context.EmitNullChecking(node.Expression.Type, returnDefaultValueLabel);
            }
            extend &= CanAssign(node.Member);
            Type memberType = GetMemberType(node.Member);
            ConstructorInfo constructor = memberType.GetConstructor(Type.EmptyTypes);
            extend &= (memberType.IsClass && constructor != null) || memberType.IsArray;
            if(!extend)
                context.EmitMemberAccess(type, node.Member, whatReturn, out resultType); // stack: [obj.member]
            else
            {
                if(node.Expression == null)
                {
                    context.EmitMemberAccess(type, node.Member, whatReturn, out resultType); // stack: [obj.member]
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
                    using(var newobj = context.DeclareLocal(memberType))
                    {
                        il.Stloc(newobj);
                        il.Ldloc(newobj);
                        context.EmitMemberAssign(type, node.Member);
                        il.Ldloc(newobj);
                    }
                    il.MarkLabel(memberIsNotNullLabel);
                }
                else
                {
                    using(var temp = context.DeclareLocal(type))
                    {
                        il.Stloc(temp);
                        il.Ldloc(temp);
                        context.EmitMemberAccess(type, node.Member, whatReturn, out resultType); // stack: [obj.member]
                        var memberIsNotNullLabel = il.DefineLabel("memberIsNotNull");
                        il.Dup();
                        il.Brtrue(memberIsNotNullLabel);
                        il.Pop();
                        il.Ldloc(temp);
                        if(!memberType.IsArray)
                            il.Newobj(constructor);
                        else
                        {
                            il.Ldc_I4(0);
                            il.Newarr(memberType.GetElementType());
                        }
                        using(var newobj = context.DeclareLocal(memberType))
                        {
                            il.Stloc(newobj);
                            il.Ldloc(newobj);
                            context.EmitMemberAssign(type, node.Member);
                            il.Ldloc(newobj);
                        }
                        il.MarkLabel(memberIsNotNullLabel);
                    }
                }
            }
            return result;
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
    }
}