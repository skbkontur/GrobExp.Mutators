using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class SwitchExpressionEmitter : ExpressionEmitter<SwitchExpression>
    {
        protected override bool Emit(SwitchExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            GroboIL il = context.Il;
            Tuple<FieldInfo, FieldInfo, int> switchCase;
            if(context.Switches.TryGetValue(node, out switchCase))
            {
                var defaultLabel = il.DefineLabel("default");
                var labels = new List<GroboIL.Label>();
                var caseLabels = new GroboIL.Label[node.Cases.Count];
                GroboIL.Label switchValueIsNullLabel = null;
                for(int index = 0; index < node.Cases.Count; index++)
                {
                    var label = il.DefineLabel("case#" + index);
                    caseLabels[index] = label;
                    foreach(var testValue in node.Cases[index].TestValues)
                    {
                        if(((ConstantExpression)testValue).Value != null)
                            labels.Add(label);
                        else
                            switchValueIsNullLabel = label;
                    }
                }
                context.EmitLoadArguments(node.SwitchValue);
                using(var switchValue = context.DeclareLocal(node.SwitchValue.Type))
                {
                    il.Stloc(switchValue);
                    if(switchValueIsNullLabel != null)
                    {
                        if(!node.SwitchValue.Type.IsNullable())
                            il.Ldloc(switchValue);
                        else
                        {
                            il.Ldloca(switchValue);
                            context.EmitHasValueAccess(node.SwitchValue.Type);
                        }
                        il.Brfalse(switchValueIsNullLabel);
                    }
                    il.Ldfld(switchCase.Item1);
                    var type = node.SwitchValue.Type.IsNullable() ? node.SwitchValue.Type.GetGenericArguments()[0] : node.SwitchValue.Type;
                    var typeCode = Type.GetTypeCode(type);
                    switch(typeCode)
                    {
                    case TypeCode.Byte:
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        if(!node.SwitchValue.Type.IsNullable())
                            il.Ldloc(switchValue);
                        else
                        {
                            il.Ldloca(switchValue);
                            context.EmitValueAccess(node.SwitchValue.Type);
                        }
                        break;
                    default:
                        if(node.SwitchValue.Type.IsValueType)
                            il.Ldloca(switchValue);
                        else
                            il.Ldloc(switchValue);
                        il.Call(typeof(object).GetMethod("GetHashCode"), node.SwitchValue.Type);
                        break;
                    }
                    using(var index = context.DeclareLocal(typeof(int)))
                    {
                        il.Ldc_I4(switchCase.Item3);
                        il.Rem(typeof(uint));
                        il.Stloc(index);
                        il.Ldloc(index);
                        il.Ldelem(type);
                        if(!node.SwitchValue.Type.IsNullable())
                            il.Ldloc(switchValue);
                        else
                        {
                            il.Ldloca(switchValue);
                            context.EmitValueAccess(node.SwitchValue.Type);
                        }
                        if(node.Comparison != null)
                            il.Call(node.Comparison);
                        else
                            il.Ceq();
                        il.Brfalse(defaultLabel);
                        il.Ldfld(switchCase.Item2);
                        il.Ldloc(index);
                        il.Ldelem(typeof(int));
                        il.Switch(labels.ToArray());
                    }
                    il.MarkLabel(defaultLabel);
                    var doneLabel = il.DefineLabel("done");
                    context.EmitLoadArguments(node.DefaultBody);
                    il.Br(doneLabel);
                    for(int index = 0; index < node.Cases.Count; ++index)
                    {
                        il.MarkLabel(caseLabels[index]);
                        context.EmitLoadArguments(node.Cases[index].Body);
                        if(index < node.Cases.Count - 1)
                            il.Br(doneLabel);
                    }
                    il.MarkLabel(doneLabel);
                }
            }
            else
            {
            }
            resultType = node.Type;
            return false;
        }
    }
}