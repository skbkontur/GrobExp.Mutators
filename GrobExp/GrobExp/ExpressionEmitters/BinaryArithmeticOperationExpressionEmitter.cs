using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class BinaryArithmeticOperationExpressionEmitter : ExpressionEmitter<BinaryExpression>
    {
        protected override bool Emit(BinaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            Expression left = node.Left;
            Expression right = node.Right;
            context.EmitLoadArguments(left, right);
            context.EmitArithmeticOperation(node.NodeType, node.Type, left.Type, right.Type, node.Method);
            resultType = node.Type;
            return false;
        }
    }
}