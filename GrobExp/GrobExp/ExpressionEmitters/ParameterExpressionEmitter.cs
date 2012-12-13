using System;
using System.Linq.Expressions;

namespace GrobExp.ExpressionEmitters
{
    internal class ParameterExpressionEmitter: ExpressionEmitter<ParameterExpression>
    {
        protected override bool Emit(ParameterExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            int index = Array.IndexOf(context.Parameters, node);
            if (index >= 0)
            {
                if (returnByRef && node.Type.IsValueType)
                {
                    context.Il.Ldarga(index); // stack: [&parameter]
                    resultType = node.Type.MakeByRefType();
                }
                else
                {
                    context.Il.Ldarg(index); // stack: [parameter]
                    resultType = node.Type;
                }
                return false;
            }
            EmittingContext.LocalHolder variable;
            if (context.VariablesToLocals.TryGetValue(node, out variable))
            {
                if (returnByRef && node.Type.IsValueType)
                {
                    context.Il.Ldloca(variable); // stack: [&variable]
                    resultType = node.Type.MakeByRefType();
                }
                else
                {
                    context.Il.Ldloc(variable); // stack: [variable]
                    resultType = node.Type;
                }
            }
            else
                throw new InvalidOperationException("Unknown parameter " + node);
            return false;
        }
    }
}