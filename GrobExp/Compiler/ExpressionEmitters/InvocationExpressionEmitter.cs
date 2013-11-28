using System;
using System.Linq;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.Compiler.ExpressionEmitters
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
                throw new InvalidOperationException();
                /*result = false;
                var lambda = (LambdaExpression)node.Expression;
                var expressions = lambda.Parameters.Select((t, i) => Expression.Assign(t, node.Arguments[i])).Cast<Expression>().ToList();
                expressions.Add(lambda.Body);
                var block = Expression.Block(lambda.Body.Type, lambda.Parameters, expressions);
                ExpressionEmittersCollection.Emit(block, context, out resultType);*/
            }
            resultType = node.Type;
            return result;
        }
    }
}