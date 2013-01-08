using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using GrEmit;

using GrobExp.ExpressionEmitters;

namespace GrobExp
{
    public static class LambdaCompiler
    {
        public static TDelegate Compile<TDelegate>(Expression<TDelegate> lambda, CompilerOptions options = CompilerOptions.All) where TDelegate : class
        {
            var compiledLambdas = new List<CompiledLambda>();
            ParameterExpression closureParameter;
            Type closureType;
            var resolvedLambda = new ExpressionClosureResolver(lambda).Resolve(out closureType, out closureParameter);
            var compiledLambda = Compile(resolvedLambda, closureType, closureParameter, options, compiledLambdas);
            var ilCode = new StringBuilder(compiledLambda.ILCode);
            for(int i = 0; i < compiledLambdas.Count; ++i)
            {
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine("delegates[" + i + "]");
                ilCode.AppendLine(compiledLambdas[i].ILCode);
            }
            Delegate result;
            if(closureParameter == null)
                result = compiledLambda.Delegate;
            else
            {
                BuildDelegatesFoister(closureType)(compiledLambdas.Concat(new[] {compiledLambda}).Select(compiledLambda1 => compiledLambda1.Delegate).ToArray());
                Type returnType = lambda.Body.Type;
                Type[] parameterTypes = lambda.Parameters.Select(parameter => parameter.Type).ToArray();
                var lambdaInvoker = DynamicMethodInvokerBuilder.BuildDynamicMethodInvoker(closureType, returnType, parameterTypes, true);
                result = CreateLambdaInvoker(lambdaInvoker, Extensions.GetDelegateType(parameterTypes, returnType), DynamicMethodInvokerBuilder.DynamicMethodPointerExtractor((DynamicMethod)compiledLambda.Method));
            }
            return (TDelegate)(object)result;
        }

        public static readonly AssemblyBuilder Assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        public static readonly ModuleBuilder Module = Assembly.DefineDynamicModule(Guid.NewGuid().ToString());

        internal static CompiledLambda Compile(LambdaExpression lambda, Type closureType, ParameterExpression closureParameter, CompilerOptions options, List<CompiledLambda> compiledLambdas)
        {
            var parameters = lambda.Parameters.ToArray();
            Type[] parameterTypes = parameters.Select(parameter => parameter.Type).ToArray();
            Type returnType = lambda.ReturnType;
            var method = new DynamicMethod(Guid.NewGuid().ToString(), MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, returnType, parameterTypes, Module, true);
            var il = new GroboIL(method);

            var context = new EmittingContext
                {
                    Options = options,
                    Parameters = parameters,
                    ClosureType = closureType,
                    ClosureParameter = closureParameter,
                    CompiledLambdas = compiledLambdas,
                    Il = il
                };
            var returnDefaultValueLabel = il.DefineLabel("returnDefaultValue");
            Type resultType;
            bool labelUsed = ExpressionEmittersCollection.Emit(lambda.Body, context, returnDefaultValueLabel, out resultType);
            if(returnType == typeof(bool) && resultType == typeof(bool?))
                context.ConvertFromNullableBoolToBool();
            if(returnType == typeof(void) && resultType != typeof(void))
            {
                using(var temp = context.DeclareLocal(resultType))
                    il.Stloc(temp);
            }
            il.Ret();
            if(labelUsed && context.CanReturn)
            {
                il.MarkLabel(returnDefaultValueLabel);
                il.Pop();
                if(returnType != typeof(void))
                {
                    if(!returnType.IsValueType)
                        il.Ldnull(returnType);
                    else
                    {
                        using(var defaultValue = context.DeclareLocal(returnType))
                        {
                            il.Ldloca(defaultValue);
                            il.Initobj(returnType);
                            il.Ldloc(defaultValue);
                        }
                    }
                }
                il.Ret();
            }
            return new CompiledLambda
                {
                    Delegate = method.CreateDelegate(Extensions.GetDelegateType(parameterTypes, returnType)),
                    Method = method,
                    ILCode = il.GetILCode()
                };
        }

        private static Action<Delegate[]> BuildDelegatesFoister(Type type)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new[] {typeof(Delegate[])}, Module, true);
            var il = new GroboIL(method);
            il.Ldarg(0);
            il.Stfld(type.GetField("delegates", BindingFlags.Public | BindingFlags.Static));
            il.Ret();
            return (Action<Delegate[]>)method.CreateDelegate(typeof(Action<Delegate[]>));
        }

        private static Delegate CreateLambdaInvoker(Type lambdaInvokerType, Type resultType, IntPtr pointer)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(Delegate), Type.EmptyTypes, Module, true);
            var il = new GroboIL(method);
            var types = new[] {typeof(IntPtr)};
            il.Ldc_IntPtr(pointer);
            il.Newobj(lambdaInvokerType.GetConstructor(types));
            il.Ldftn(lambdaInvokerType.GetMethod("Invoke"));
            il.Newobj(resultType.GetConstructor(new[] {typeof(object), typeof(IntPtr)}));
            il.Ret();
            return ((Func<Delegate>)method.CreateDelegate(typeof(Func<Delegate>)))();
        }
    }
}