using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class InvocationExpressionEmitter : ExpressionEmitter<InvocationExpression>
    {
        protected override bool Emit(InvocationExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            bool result;
            if(node.Expression.NodeType != ExpressionType.Lambda)
            {
                Type delegateType;
                result = ExpressionEmittersCollection.Emit(node.Expression, context, returnDefaultValueLabel, ResultType.Value, extend, out delegateType);
                context.EmitLoadArguments(node.Arguments.ToArray());
                var invokeMethod = delegateType.GetMethod("Invoke", node.Arguments.Select(argument => argument.Type).ToArray());
                context.Il.Call(invokeMethod, delegateType);
            }
            else
            {
                result = false;
                Type delegateType;
                var pointer = LambdaExpressionEmitter.Compile((LambdaExpression)node.Expression, context, out delegateType);
                bool needClosure = context.ClosureParameter != null;
                GroboIL il = context.Il;
                if(!needClosure)
                    throw new NotSupportedException();
                else
                {
                    Type closureType;
                    ExpressionEmittersCollection.Emit(context.ClosureParameter, context, out closureType);
                    context.EmitLoadArguments(node.Arguments.ToArray());
                    il.Ldc_IntPtr(pointer);
                    il.Calli(CallingConventions.Standard, node.Type, new[] {closureType}.Concat(node.Arguments.Select(argument => argument.Type)).ToArray());
                }
            }
            resultType = node.Type;
            return result;
        }
    }
}