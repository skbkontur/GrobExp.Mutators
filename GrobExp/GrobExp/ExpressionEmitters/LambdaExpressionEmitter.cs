using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class LambdaExpressionEmitter : ExpressionEmitter<LambdaExpression>
    {
        protected override bool Emit(LambdaExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var parameterTypes = node.Parameters.Select(parameter => parameter.Type).ToArray();
            resultType = Extensions.GetDelegateType(parameterTypes, node.ReturnType);

            GroboIL il = context.Il;
            bool needClosure = context.ClosureParameter != null;
            if(!needClosure)
                throw new NotSupportedException();
            else
            {
                var parameters = new[] {context.ClosureParameter}.Concat(node.Parameters).ToArray();
                var lambda = Expression.Lambda(Extensions.GetDelegateType(parameters.Select(parameter => parameter.Type).ToArray(), node.ReturnType), node.Body, node.Name, node.TailCall, parameters);
                CompiledLambda compiledLambda;
                if(context.TypeBuilder == null)
                    compiledLambda = LambdaCompiler.CompileInternal(lambda, context.DebugInfoGenerator, context.ClosureType, context.ClosureParameter, context.Switches, context.Options, context.CompiledLambdas);
                else
                {
                    var method = context.TypeBuilder.DefineMethod(Guid.NewGuid().ToString(), MethodAttributes.Public | MethodAttributes.Static, lambda.ReturnType, lambda.Parameters.Select(parameter => parameter.Type).ToArray());
                    var ilCode = LambdaCompiler.CompileInternal(lambda, context.DebugInfoGenerator, context.ClosureType, context.ClosureParameter, context.Switches, context.Options, context.CompiledLambdas, method);
                    compiledLambda = new CompiledLambda {Method = method, ILCode = ilCode};
                }
                context.CompiledLambdas.Add(compiledLambda);
                Type closureType;
                ExpressionEmittersCollection.Emit(context.ClosureParameter, context, out closureType);

                if(context.TypeBuilder != null)
                    il.Ldftn(compiledLambda.Method);
                else
                {
                    var subLambdaInvoker = DynamicMethodInvokerBuilder.BuildDynamicMethodInvoker(closureType, node.Body.Type, parameterTypes);
                    var pointer = DynamicMethodInvokerBuilder.DynamicMethodPointerExtractor((DynamicMethod)compiledLambda.Method);
                    il.Ldc_IntPtr(pointer);
                    var types = new[] {closureType, typeof(IntPtr)};
                    il.Newobj(subLambdaInvoker.GetConstructor(types));
                    il.Ldftn(subLambdaInvoker.GetMethod("Invoke"));
                }

                il.Newobj(resultType.GetConstructor(new[] {typeof(object), typeof(IntPtr)}));
            }
            return false;
        }
    }
}