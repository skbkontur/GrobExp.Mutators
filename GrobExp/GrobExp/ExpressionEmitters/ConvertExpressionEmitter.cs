using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.ExpressionEmitters
{
    internal class ConvertExpressionEmitter : ExpressionEmitter<UnaryExpression>
    {
        protected override bool Emit(UnaryExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var result = ExpressionEmittersCollection.Emit(node.Operand, context, returnDefaultValueLabel, false, extend, out resultType); // stack: [obj]
            if(resultType != node.Type && !(context.Options.HasFlag(CompilerOptions.UseTernaryLogic) && resultType == typeof(bool?) && node.Type == typeof(bool)))
            {
                if(node.Method != null)
                    context.Il.Call(node.Method);
                else
                    EmitConvert(context, node.Operand.Type, node.Type); // stack: [(type)obj]
                resultType = node.Type;
            }
            return result;
        }

        private static void EmitConvert(EmittingContext context, Type from, Type to)
        {
            if(from == to) return;
            var il = context.Il;
            if(!from.IsValueType)
            {
                if(!to.IsValueType)
                    il.Castclass(to);
                else
                {
                    if(from != typeof(object))
                        throw new InvalidCastException("Cannot cast an object of type '" + from + "' to type '" + to + "'");
                    il.Unbox_Any(to);
                }
            }
            else
            {
                if(!to.IsValueType)
                {
                    if(to != typeof(object))
                        throw new InvalidCastException("Cannot cast an object of type '" + from + "' to type '" + to + "'");
                    il.Box(from);
                }
                else
                {
                    if(to.IsNullable())
                    {
                        var toArgument = to.GetGenericArguments()[0];
                        if(from.IsNullable())
                        {
                            var fromArgument = from.GetGenericArguments()[0];
                            using(var temp = context.DeclareLocal(from))
                            {
                                il.Stloc(temp);
                                il.Ldloca(temp);
                                FieldInfo hasValueField = from.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                                il.Ldfld(hasValueField);
                                var valueIsNullLabel = il.DefineLabel("valueIsNull");
                                il.Brfalse(valueIsNullLabel);
                                il.Ldloca(temp);
                                FieldInfo valueField = from.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                                il.Ldfld(valueField);
                                if(toArgument != fromArgument)
                                    EmitConvert(context, fromArgument, toArgument);
                                il.Newobj(to.GetConstructor(new[] {toArgument}));
                                var doneLabel = il.DefineLabel("done");
                                il.Br(doneLabel);
                                il.MarkLabel(valueIsNullLabel);
                                context.EmitLoadDefaultValue(to);
                                il.MarkLabel(doneLabel);
                            }
                        }
                        else
                        {
                            if(toArgument != from)
                                EmitConvert(context, from, toArgument);
                            il.Newobj(to.GetConstructor(new[] {toArgument}));
                        }
                    }
                    else if(to.IsEnum)
                        EmitConvert(context, from, typeof(int));
                    else if(from.IsEnum)
                        EmitConvert(context, typeof(int), to);
                    else
                        throw new NotSupportedException("Cast from type '" + from + "' to type '" + to + "' is not supported");
                }
            }
        }
    }
}