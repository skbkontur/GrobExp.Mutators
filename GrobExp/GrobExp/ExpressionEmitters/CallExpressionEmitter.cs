using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class CallExpressionEmitter : ExpressionEmitter<MethodCallExpression>
    {
        protected override bool Emit(MethodCallExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var result = false;
            GroboIL il = context.Il;
            var method = node.Method;
            Expression obj;
            IEnumerable<Expression> arguments;
            bool isStatic = method.IsStatic;
            if(!isStatic)
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
                result |= ExpressionEmittersCollection.Emit(obj, context, returnDefaultValueLabel, isStatic ? ResultType.Value : ResultType.ByRefValueTypesOnly, extend, out actualType); // stack: [obj]
                if(actualType.IsValueType && !isStatic)
                {
                    using(var temp = context.DeclareLocal(actualType))
                    {
                        il.Stloc(temp);
                        il.Ldloca(temp);
                    }
                    actualType = actualType.MakeByRefType();
                }
                if(context.Options.HasFlag(CompilerOptions.CheckNullReferences) && !actualType.IsValueType)
                    result |= context.EmitNullChecking(type, returnDefaultValueLabel);
            }
            var parameters = method.GetParameters();
            var argumentsArray = arguments.ToArray();
            for(int i = 0; i < argumentsArray.Length; i++)
            {
                var argument = argumentsArray[i];
                var parameter = parameters[i];
                if (parameter.ParameterType.IsByRef)
                {
                    Type argumentType;
                    var options = context.Options;
                    context.Options = CompilerOptions.None;
                    ExpressionEmittersCollection.Emit(argument, context, null, ResultType.ByRefAll, false, out argumentType);
                    context.Options = options;
                    if(!argumentType.IsByRef)
                        throw new InvalidOperationException("Expected type by reference");
                }
                else
                {
                    Type argumentType;
                    context.EmitLoadArgument(argument, true, out argumentType);
                }
            }
            il.Call(method, type);
            resultType = node.Type;
            return result;
        }
    }
}