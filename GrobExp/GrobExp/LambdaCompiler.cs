using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

using GrEmit;

using GrobExp.ExpressionEmitters;

namespace GrobExp
{
    public static class LambdaCompiler
    {
        public static Delegate Compile(LambdaExpression lambda, CompilerOptions options = CompilerOptions.All)
        {
            string il;
            return CompileInternal(lambda, null, out il, options);
        }

        public static Delegate Compile(LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, CompilerOptions options = CompilerOptions.All)
        {
            string il;
            return CompileInternal(lambda, debugInfoGenerator, out il, options);
        }

        public static Delegate Compile(LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, out string il, CompilerOptions options = CompilerOptions.All)
        {
            return CompileInternal(lambda, debugInfoGenerator, out il, options);
        }

        public static TDelegate Compile<TDelegate>(Expression<TDelegate> lambda, CompilerOptions options = CompilerOptions.All) where TDelegate : class
        {
            string il;
            return Compile(lambda, null, out il, options);
        }

        public static TDelegate Compile<TDelegate>(Expression<TDelegate> lambda, out string il, CompilerOptions options = CompilerOptions.All) where TDelegate : class
        {
            return Compile(lambda, null, out il, options);
        }

        public static TDelegate Compile<TDelegate>(Expression<TDelegate> lambda, DebugInfoGenerator debugInfoGenerator, out string il, CompilerOptions options = CompilerOptions.All) where TDelegate : class
        {
            return (TDelegate)(object)CompileInternal(lambda, debugInfoGenerator, out il, options);
        }

        public static void CompileToMethod(LambdaExpression lambda, MethodBuilder method, CompilerOptions options = CompilerOptions.All)
        {
            CompileToMethod(lambda, method, null, options);
        }

        public static void CompileToMethod(LambdaExpression lambda, MethodBuilder method, DebugInfoGenerator debugInfoGenerator, CompilerOptions options = CompilerOptions.All)
        {
            var compiledLambdas = new List<CompiledLambda>();
            Type closureType;
            ParameterExpression closureParameter;
            Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches;
            var module = method.Module as ModuleBuilder;
            if (module == null)
                throw new ArgumentException("Unable to obtain module builder of the method", "method");
            method.SetReturnType(lambda.ReturnType);
            method.SetParameters(lambda.Parameters.Select(parameter => parameter.Type).ToArray());
            var resolvedLambda = new ExpressionClosureResolver(lambda, module).Resolve(out closureType, out closureParameter, out switches);
            var ilCode = new StringBuilder(CompileInternal(resolvedLambda, debugInfoGenerator, closureType, closureParameter, switches, options, compiledLambdas, method));
            for(int i = 0; i < compiledLambdas.Count; ++i)
            {
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine("delegates[" + i + "]");
                ilCode.AppendLine(compiledLambdas[i].ILCode);
            }
        }

        internal static CompiledLambda CompileInternal(LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, Type closureType, ParameterExpression closureParameter, Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches, CompilerOptions options, List<CompiledLambda> compiledLambdas)
        {
            var parameters = lambda.Parameters.ToArray();
            Type[] parameterTypes = parameters.Select(parameter => parameter.Type).ToArray();
            Type returnType = lambda.ReturnType;
            var method = new DynamicMethod(Guid.NewGuid().ToString(), MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, returnType, parameterTypes, Module, true);
            var il = new GroboIL(method);

            var context = new EmittingContext
                {
                    Options = options,
                    DebugInfoGenerator = debugInfoGenerator,
                    Lambda = lambda,
                    Method = method,
                    SkipVisibility = true,
                    Parameters = parameters,
                    ClosureType = closureType,
                    ClosureParameter = closureParameter,
                    Switches = switches,
                    CompiledLambdas = compiledLambdas,
                    Il = il
                };
            var returnDefaultValueLabel = context.CanReturn ? il.DefineLabel("returnDefaultValue") : null;
            Type resultType;
            bool labelUsed = ExpressionEmittersCollection.Emit(lambda.Body, context, returnDefaultValueLabel, returnType == typeof(void) ? ResultType.Void : ResultType.Value, false, out resultType);
            if(returnType == typeof(bool) && resultType == typeof(bool?))
                context.ConvertFromNullableBoolToBool();
            if(returnType == typeof(void) && resultType != typeof(void))
            {
                using(var temp = context.DeclareLocal(resultType))
                    il.Stloc(temp);
            }
            il.Ret();
            if(labelUsed)
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

        internal static string CompileInternal(LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, Type closureType, ParameterExpression closureParameter, Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches, CompilerOptions options, List<CompiledLambda> compiledLambdas, MethodBuilder method)
        {
            var typeBuilder = method.ReflectedType as TypeBuilder;
            if(typeBuilder == null)
                throw new ArgumentException("Unable to obtain type builder of the method", "method");
            Type returnType = lambda.ReturnType;
            var il = new GroboIL(method);

            var context = new EmittingContext
                {
                    Options = options,
                    DebugInfoGenerator = debugInfoGenerator,
                    TypeBuilder = typeBuilder,
                    Lambda = lambda,
                    Method = method,
                    SkipVisibility = false,
                    Parameters = lambda.Parameters.ToArray(),
                    ClosureType = closureType,
                    ClosureParameter = closureParameter,
                    Switches = switches,
                    CompiledLambdas = compiledLambdas,
                    Il = il
                };
            var returnDefaultValueLabel = context.CanReturn ? il.DefineLabel("returnDefaultValue") : null;
            Type resultType;
            bool labelUsed = ExpressionEmittersCollection.Emit(lambda.Body, context, returnDefaultValueLabel, returnType == typeof(void) ? ResultType.Void : ResultType.Value, false, out resultType);
            if(returnType == typeof(bool) && resultType == typeof(bool?))
                context.ConvertFromNullableBoolToBool();
            if(returnType == typeof(void) && resultType != typeof(void))
            {
                using(var temp = context.DeclareLocal(resultType))
                    il.Stloc(temp);
            }
            il.Ret();
            if(labelUsed)
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
            return il.GetILCode();
        }

        internal static readonly AssemblyBuilder Assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        internal static readonly ModuleBuilder Module = Assembly.DefineDynamicModule(Guid.NewGuid().ToString());

        private static Delegate CompileInternal(LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, out string il, CompilerOptions options = CompilerOptions.All)
        {
            var compiledLambdas = new List<CompiledLambda>();
            Type closureType;
            ParameterExpression closureParameter;
            Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches;
            var resolvedLambda = new ExpressionClosureResolver(lambda, Module).Resolve(out closureType, out closureParameter, out switches);
            var compiledLambda = CompileInternal(resolvedLambda, debugInfoGenerator, closureType, closureParameter, switches, options, compiledLambdas);
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
            il = ilCode.ToString();
            Delegate result;
            if(closureType == null)
                result = compiledLambda.Delegate;
            else
            {
                BuildDelegatesFoister(closureType)(compiledLambdas.Concat(new[] {compiledLambda}).Select(compiledLambda1 => compiledLambda1.Delegate).ToArray());
                Type returnType = lambda.ReturnType;
                Type[] parameterTypes = lambda.Parameters.Select(parameter => parameter.Type).ToArray();
                var lambdaInvoker = DynamicMethodInvokerBuilder.BuildDynamicMethodInvoker(closureType, returnType, parameterTypes, true);
                result = CreateLambdaInvoker(lambdaInvoker, Extensions.GetDelegateType(parameterTypes, returnType), DynamicMethodInvokerBuilder.DynamicMethodPointerExtractor((DynamicMethod)compiledLambda.Method));
            }
            return result;
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
            il.Ldc_IntPtr(pointer);
            il.Newobj(lambdaInvokerType.GetConstructor(new[] {typeof(IntPtr)}));
            il.Ldftn(lambdaInvokerType.GetMethod("Invoke"));
            il.Newobj(resultType.GetConstructor(new[] {typeof(object), typeof(IntPtr)}));
            il.Ret();
            return ((Func<Delegate>)method.CreateDelegate(typeof(Func<Delegate>)))();
        }
    }
}