using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ConvertExpressionEmitter : ExpressionEmitter<UnaryExpression>
    {
        protected override bool Emit(UnaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var result = ExpressionEmittersCollection.Emit(node.Operand, context, returnDefaultValueLabel, ResultType.Value, extend, out resultType); // stack: [obj]
            if(resultType != node.Type && !(context.Options.HasFlag(CompilerOptions.UseTernaryLogic) && resultType == typeof(bool?) && node.Type == typeof(bool)))
            {
                if(node.Method != null)
                    context.Il.Call(node.Method);
                else
                {
                    switch(node.NodeType)
                    {
                    case ExpressionType.Convert:
                        context.EmitConvert(node.Operand.Type, node.Type); // stack: [(type)obj]
                        break;
                    case ExpressionType.ConvertChecked:
                        context.EmitConvert(node.Operand.Type, node.Type, true); // stack: [(type)obj]
                        break;
                    default:
                        throw new InvalidOperationException("Node type '" + node.NodeType + "' is not valid at this point");
                    }
                }
                resultType = node.Type;
            }
            return result;
        }
    }
}