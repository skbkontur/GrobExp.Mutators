using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class NotExpressionEmitter : ExpressionEmitter<UnaryExpression>
    {
        protected override bool Emit(UnaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            GroboIL il = context.Il;
            if(node.Method != null)
                throw new NotSupportedException("Custom operator '" + node.NodeType + "' is not supported");
            var operand = node.Operand;

            context.EmitLoadArgument(operand, false, out resultType);
            if(resultType == typeof(bool))
            {
                il.Ldc_I4(1);
                il.Xor();
            }
            else if(resultType == typeof(bool?))
            {
                using(var value = context.DeclareLocal(typeof(bool?)))
                {
                    il.Stloc(value);
                    il.Ldloca(value);
                    il.Ldfld(nullableBoolHasValueField);
                    var returnLabel = il.DefineLabel("return");
                    il.Brfalse(returnLabel);
                    il.Ldloca(value);
                    il.Ldfld(nullableBoolValueField);
                    il.Ldc_I4(1);
                    il.Xor();
                    il.Newobj(nullableBoolConstructor);
                    il.Stloc(value);
                    il.MarkLabel(returnLabel);
                    il.Ldloc(value);
                }
            }
            else throw new InvalidOperationException("Cannot perform '" + node.NodeType + "' operator on type '" + resultType + "'");
            return false;
        }

        // ReSharper disable RedundantExplicitNullableCreation
        private static readonly ConstructorInfo nullableBoolConstructor = ((NewExpression)((Expression<Func<bool, bool?>>)(b => new bool?(b))).Body).Constructor;
        // ReSharper restore RedundantExplicitNullableCreation
        private static readonly FieldInfo nullableBoolValueField = typeof(bool?).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo nullableBoolHasValueField = typeof(bool?).GetField("hasValue", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}