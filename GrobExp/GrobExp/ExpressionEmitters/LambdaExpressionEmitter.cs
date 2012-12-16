using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class LambdaExpressionEmitter : ExpressionEmitter<LambdaExpression>
    {
        protected override bool Emit(LambdaExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, bool returnByRef, bool extend, out Type resultType)
        {
            var parameterTypes = node.Parameters.Select(parameter => parameter.Type).ToArray();
            resultType = Extensions.GetDelegateType(parameterTypes, node.ReturnType);

            GroboIL il = context.Il;
            bool needClosure = context.ClosureParameter != null;
            if(!needClosure)
            {
                /*var compiledLambda = LambdaCompiler.Compile(node, context.ClosureType, null, context.Options, context.CompiledLambdas);
                context.CompiledLambdas.Add(compiledLambda);
                il.Ldnull();
                il.Ldfld(Closure.DelegatesField);
                il.Ldc_I4(context.CompiledLambdas.Count - 1);
                il.Ldelem(typeof(Delegate));*/
                throw new NotSupportedException();
            }
            else
            {
                var compiledLambda = LambdaCompiler.Compile(Expression.Lambda(node.Body, new[] {context.ClosureParameter}.Concat(node.Parameters)), context.ClosureType, context.ClosureParameter, context.Options, context.CompiledLambdas);
                Type closureType;
                ExpressionEmittersCollection.Emit(context.ClosureParameter, context, out closureType);
                context.CompiledLambdas.Add(compiledLambda);

                var subLambdaInvoker = DynamicMethodInvokerBuilder.BuildDynamicMethodInvoker(closureType, node.Body.Type, parameterTypes);
                var pointer = DynamicMethodInvokerBuilder.DynamicMethodPointerExtractor((DynamicMethod)compiledLambda.Method);
                il.Ldc_IntPtr(pointer);
                var types = new[] {closureType, typeof(IntPtr)};
                il.Newobj(subLambdaInvoker.GetConstructor(types));
                il.Ldftn(subLambdaInvoker.GetMethod("Invoke"));

                il.Newobj(resultType.GetConstructor(new[] {typeof(object), typeof(IntPtr)}));
            }
            return false;
        }
    }
}