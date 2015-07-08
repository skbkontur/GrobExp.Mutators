using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GrobExp.Compiler.ExpressionEmitters
{
    internal class LambdaExpressionEmitter : ExpressionEmitter<LambdaExpression>
    {
        protected override bool EmitInternal(LambdaExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            var parameterTypes = node.Parameters.Select(parameter => parameter.Type).ToArray();
            resultType = Extensions.GetDelegateType(parameterTypes, node.ReturnType);

            GroboIL il = context.Il;
            bool needClosure = context.ClosureParameter != null;
            var needConstants = context.ConstantsParameter != null;
            {
                List<ParameterExpression> parameters = new List<ParameterExpression>();
                if(needConstants)
                    parameters.Add(context.ConstantsParameter);
                if(needClosure)
                    parameters.Add(context.ClosureParameter);
                parameters.AddRange(node.Parameters);
                var lambda = Expression.Lambda(Extensions.GetDelegateType(parameters.Select(parameter => parameter.Type).ToArray(), node.ReturnType), node.Body, node.Name, node.TailCall, parameters);
                CompiledLambda compiledLambda;
                if(context.TypeBuilder == null)
                    compiledLambda = LambdaCompiler.CompileInternal(lambda, context.DebugInfoGenerator, context.ClosureType, context.ClosureParameter, context.ConstantsType, context.ConstantsParameter, null, context.Switches, context.Options, context.CompiledLambdas);
                else
                {
                    var method = context.TypeBuilder.DefineMethod(Guid.NewGuid().ToString(), MethodAttributes.Public | MethodAttributes.Static, lambda.ReturnType, lambda.Parameters.Select(parameter => parameter.Type).ToArray());
                    var ilCode = LambdaCompiler.CompileInternal(lambda, context.DebugInfoGenerator, context.ClosureType, context.ClosureParameter, context.Switches, context.Options, context.CompiledLambdas, method);
                    compiledLambda = new CompiledLambda {Method = method, ILCode = ilCode};
                }
                context.CompiledLambdas.Add(compiledLambda);
                if(needConstants)
                {
                    Type constantsType;
                    ExpressionEmittersCollection.Emit(context.ConstantsParameter, context, out constantsType);
                }
                if(needClosure)
                {
                    Type closureType;
                    ExpressionEmittersCollection.Emit(context.ClosureParameter, context, out closureType);
                }

                {
                    var subLambdaInvoker = needClosure
                                               ? DynamicMethodInvokerBuilder.BuildDynamicMethodInvoker(context.ConstantsType, context.ClosureType, node.Body.Type, parameterTypes)
                                               : DynamicMethodInvokerBuilder.BuildDynamicMethodInvoker(context.ConstantsType, node.Body.Type, parameterTypes);
                    if(context.TypeBuilder != null)
                        il.Ldftn(compiledLambda.Method);
                    else
                    {
                        var pointer = DynamicMethodInvokerBuilder.DynamicMethodPointerExtractor((DynamicMethod)compiledLambda.Method);
                        il.Ldc_IntPtr(pointer);
                    }
                    var types = needClosure
                                    ? new[] {context.ConstantsType, context.ClosureType, typeof(IntPtr)}
                                    : new[] {context.ConstantsType, typeof(IntPtr)};
                    il.Newobj(subLambdaInvoker.GetConstructor(types));
                    il.Ldftn(subLambdaInvoker.GetMethod("Invoke"));
                }

                il.Newobj(resultType.GetConstructor(new[] {typeof(object), typeof(IntPtr)}));
            }
            return false;
        }
    }
}