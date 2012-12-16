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
                var compiledLambda = LambdaCompiler.Compile(node, context.ClosureType, null, context.Options, context.CompiledLambdas);
                context.CompiledLambdas.Add(compiledLambda);
                il.Ldnull();
                il.Ldfld(context.ClosureType.GetField("delegates", BindingFlags.Public | BindingFlags.Static));
                il.Ldc_I4(context.CompiledLambdas.Count - 1);
                il.Ldelem(typeof(Delegate));
            }
            else
            {
                var compiledLambda = LambdaCompiler.Compile(Expression.Lambda(node.Body, new[] {context.ClosureParameter}.Concat(node.Parameters)), context.ClosureType, context.ClosureParameter, context.Options, context.CompiledLambdas);
                Type closureType;
                ExpressionEmittersCollection.Emit(context.ClosureParameter, context, out closureType);
                context.CompiledLambdas.Add(compiledLambda);

                var subLambdaInvoker = DynamicMethodInvokerBuilder.BuildDynamicMethodInvoker(closureType, node.Body.Type, parameterTypes);
                var pointer = dynamicMethodPointerExtractor((DynamicMethod)compiledLambda.Method);
                il.Ldc_IntPtr(pointer);
                var types = new[] {closureType, typeof(IntPtr)};
                il.Newobj(subLambdaInvoker.GetConstructor(types));
                il.Ldftn(subLambdaInvoker.GetMethod("Invoke"));

                il.Newobj(resultType.GetConstructor(new[] {typeof(object), typeof(IntPtr)}));
            }
            return false;
        }

        private static Func<DynamicMethod, IntPtr> EmitDynamicMethodPointerExtractor()
        {
            var method = new DynamicMethod("DynamicMethodPointerExtractor", typeof(IntPtr), new[] {typeof(DynamicMethod)}, typeof(LambdaExpressionEmitter).Module, true);
            var il = new GroboIL(method);
            il.Ldarg(0); // stack: [dynamicMethod]
            MethodInfo getMethodDescriptorMethod = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
            if(getMethodDescriptorMethod == null)
                throw new MissingMethodException(typeof(DynamicMethod).Name, "GetMethodDescriptor");
            il.Call(getMethodDescriptorMethod); // stack: [dynamicMethod.GetMethodDescriptor()]
            var runtimeMethodHandle = il.DeclareLocal(typeof(RuntimeMethodHandle));
            il.Stloc(runtimeMethodHandle); // runtimeMethodHandle = dynamicMethod.GetMethodDescriptor(); stack: []
            il.Ldloc(runtimeMethodHandle); // stack: [runtimeMethodHandle]
            MethodInfo prepareMethodMethod = typeof(RuntimeHelpers).GetMethod("PrepareMethod", new[] {typeof(RuntimeMethodHandle)});
            if(prepareMethodMethod == null)
                throw new MissingMethodException(typeof(RuntimeHelpers).Name, "PrepareMethod");
            il.Call(prepareMethodMethod); // RuntimeHelpers.PrepareMethod(runtimeMethodHandle)
            MethodInfo getFunctionPointerMethod = typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", BindingFlags.Instance | BindingFlags.Public);
            if(getFunctionPointerMethod == null)
                throw new MissingMethodException(typeof(RuntimeMethodHandle).Name, "GetFunctionPointer");
            il.Ldloca(runtimeMethodHandle); // stack: [&runtimeMethodHandle]
            il.Call(getFunctionPointerMethod); // stack: [runtimeMethodHandle.GetFunctionPointer()]
            il.Ret(); // return runtimeMethodHandle.GetFunctionPointer()
            return (Func<DynamicMethod, IntPtr>)method.CreateDelegate(typeof(Func<DynamicMethod, IntPtr>));
        }

        private static readonly Func<DynamicMethod, IntPtr> dynamicMethodPointerExtractor = EmitDynamicMethodPointerExtractor();
    }
}