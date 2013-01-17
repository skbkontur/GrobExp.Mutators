﻿using System;
using System.Linq.Expressions;

using GrEmit;

using System.Linq;

namespace GrobExp.ExpressionEmitters
{
    internal class ListInitExpressionEmitter: ExpressionEmitter<ListInitExpression>
    {
        protected override bool Emit(ListInitExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var il = context.Il;
            var result = ExpressionEmittersCollection.Emit(node.NewExpression, context, returnDefaultValueLabel, out resultType);
            if (!resultType.IsValueType)
            {
                foreach(var initializer in node.Initializers)
                {
                    il.Dup();
                    context.EmitLoadArguments(initializer.Arguments.ToArray());
                    il.Call(initializer.AddMethod, resultType);
                }
            }
            else
            {
                if(node.Initializers.Count > 0)
                {
                    using(var temp = context.DeclareLocal(resultType))
                    {
                        il.Stloc(temp);
                        foreach (var initializer in node.Initializers)
                        {
                            il.Ldloca(temp);
                            context.EmitLoadArguments(initializer.Arguments.ToArray());
                            il.Call(initializer.AddMethod, resultType);
                        }
                        il.Ldloc(temp);
                    }
                }
            }
            return result;
        }
    }
}