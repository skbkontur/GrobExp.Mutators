using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace GrobExp.ExpressionEmitters
{
    internal class CallExpressionEmitter : ExpressionEmitter<MethodCallExpression>
    {
        protected override bool Emit(MethodCallExpression node, EmittingContext context, GrobIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var result = false;
            GrobIL il = context.Il;
            var method = node.Method;
            Expression obj;
            IEnumerable<Expression> arguments;
            if(!method.IsStatic)
            {
                obj = node.Object;
                arguments = node.Arguments;
            }
            else if(method.GetCustomAttributes(typeof(ExtensionAttribute), false).Any())
            {
                obj = node.Arguments[0];
                arguments = node.Arguments.Skip(1);
            }
            else
            {
                obj = null;
                arguments = node.Arguments;
            }
            Type type = obj == null ? null : obj.Type;
            if(obj != null)
            {
                Type actualType;
                result |= ExpressionEmittersCollection.Emit(obj, context, returnDefaultValueLabel, true, extend, out actualType); // stack: [obj]
                if(actualType.IsValueType)
                {
                    using(var temp = context.DeclareLocal(actualType))
                    {
                        il.Stloc(temp);
                        il.Ldloca(temp);
                    }
                }
                if(context.Options.HasFlag(CompilerOptions.CheckNullReferences))
                    result |= context.EmitNullChecking(type, returnDefaultValueLabel);
            }
            context.EmitLoadArguments(arguments.ToArray());
            il.Call(method, type);
            resultType = node.Type;
            return result;
        }
    }
}