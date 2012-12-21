using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class EqualityExpressionEmitter : ExpressionEmitter<BinaryExpression>
    {
        protected override bool Emit(BinaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            Expression left = node.Left;
            Expression right = node.Right;
            context.EmitLoadArguments(left, right);
            GroboIL il = context.Il;
            if(!left.Type.IsNullable() && !right.Type.IsNullable())
            {
                if(node.Method != null)
                    il.Call(node.Method);
                else
                {
                    if(left.Type.IsStruct() || right.Type.IsStruct())
                        throw new InvalidOperationException("Cannot compare structs");
                    il.Ceq();
                    if(node.NodeType == ExpressionType.NotEqual)
                    {
                        il.Ldc_I4(1);
                        il.Xor();
                    }
                }
            }
            else
            {
                var type = left.Type;
                if(type != right.Type)
                    throw new InvalidOperationException("Cannot compare objects of different types '" + left.Type + "' and '" + right.Type + "'");
                using(var localLeft = context.DeclareLocal(type))
                using(var localRight = context.DeclareLocal(type))
                {
                    il.Stloc(localRight);
                    il.Stloc(localLeft);
                    if(node.Method != null)
                    {
                        FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldloca(localLeft); // stack: [&left]
                        il.Ldfld(hasValueField); // stack: [left.HasValue]
                        il.Dup(); // stack: [left.HasValue, left.HasValue]
                        il.Ldloca(localRight); // stack: [left.HasValue, left.HasValue, &right]
                        il.Ldfld(hasValueField); // stack: [left.HasValue, left.HasValue, right.HasValue]
                        var notEqualLabel = il.DefineLabel("notEqual");
                        il.Bne(notEqualLabel); // stack: [left.HasValue]
                        var equalLabel = il.DefineLabel("equal");
                        il.Brfalse(equalLabel);
                        FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldloca(localLeft);
                        il.Ldfld(valueField);
                        il.Ldloca(localRight);
                        il.Ldfld(valueField);
                        il.Call(node.Method);
                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(notEqualLabel);
                        il.Pop();
                        il.Ldc_I4(node.NodeType == ExpressionType.Equal ? 0 : 1);
                        il.Br(doneLabel);
                        il.MarkLabel(equalLabel);
                        il.Ldc_I4(node.NodeType == ExpressionType.Equal ? 1 : 0);
                        il.MarkLabel(doneLabel);
                    }
                    else
                    {
                        il.Ldloca(localLeft);
                        FieldInfo valueField = type.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldfld(valueField);
                        il.Ldloca(localRight);
                        il.Ldfld(valueField);
                        var notEqualLabel = il.DefineLabel("notEqual");
                        il.Bne(notEqualLabel);
                        il.Ldloca(localLeft);
                        FieldInfo hasValueField = type.GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
                        il.Ldfld(hasValueField);
                        il.Ldloca(localRight);
                        il.Ldfld(hasValueField);
                        il.Ceq();
                        var doneLabel = il.DefineLabel("done");
                        il.Br(doneLabel);
                        il.MarkLabel(notEqualLabel);
                        il.Ldc_I4(0);
                        il.MarkLabel(doneLabel);
                        if(node.NodeType == ExpressionType.NotEqual)
                        {
                            il.Ldc_I4(1);
                            il.Xor();
                        }
                    }
                }
            }
            resultType = typeof(bool);
            return false;
        }
    }
}