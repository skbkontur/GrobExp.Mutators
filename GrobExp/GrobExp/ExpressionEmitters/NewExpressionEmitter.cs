using System;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.ExpressionEmitters
{
    internal class NewExpressionEmitter : ExpressionEmitter<NewExpression>
    {
        protected override bool Emit(NewExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            context.EmitLoadArguments(node.Arguments.ToArray());
            // note ich: баг решарпера
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            GrobIL il = context.Il;
            if(node.Constructor != null)
                il.Newobj(node.Constructor);
            else
            {
                if(node.Type.IsValueType)
                {
                    using(var temp = context.DeclareLocal(node.Type))
                    {
                        il.Ldloca(temp);
                        il.Initobj(node.Type);
                        il.Ldloc(temp);
                    }
                }
                else throw new InvalidOperationException("Missing constructor for type '" + node.Type + "'");
            }
            resultType = node.Type;
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            // ReSharper restore HeuristicUnreachableCode
            return false;
        }
    }
}