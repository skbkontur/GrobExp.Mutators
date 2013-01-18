using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class RuntimeVariablesExpressionEmitter : ExpressionEmitter<RuntimeVariablesExpression>
    {
        protected override bool Emit(RuntimeVariablesExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            resultType = typeof(IRuntimeVariables);
            var il = context.Il;
            il.Ldc_I4(node.Variables.Count);
            il.Newarr(typeof(object));
            for(int index = 0; index < node.Variables.Count; index++)
            {
                var variable = node.Variables[index];
                il.Dup();
                il.Ldc_I4(index);
                Type variableType;
                ExpressionEmittersCollection.Emit(variable, context, out variableType);
                if(variableType.IsValueType)
                    il.Box(variableType);
                il.Stelem(typeof(object));
            }
            il.Newobj(typeof(RuntimeVariables).GetConstructor(new[] {typeof(object[])}));
            return false;
        }
    }
}