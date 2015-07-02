using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

using GrEmit;

namespace GrobExp.Compiler
{
    public static class LambdaCompiler
    {
        public static Delegate Compile(LambdaExpression lambda, CompilerOptions options)
        {
            CompiledLambda[] subLambdas;
            return CompileInternal(lambda, null, out subLambdas, options).Delegate;
        }

        public static Delegate Compile(LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, CompilerOptions options)
        {
            CompiledLambda[] subLambdas;
            return CompileInternal(lambda, debugInfoGenerator, out subLambdas, options).Delegate;
        }

        public static TDelegate Compile<TDelegate>(Expression<TDelegate> lambda, CompilerOptions options) where TDelegate : class
        {
            return Compile(lambda, null, options);
        }

        public static TDelegate Compile<TDelegate>(Expression<TDelegate> lambda, DebugInfoGenerator debugInfoGenerator, CompilerOptions options) where TDelegate : class
        {
            CompiledLambda[] subLambdas;
            return (TDelegate)(object)CompileInternal(lambda, debugInfoGenerator, out subLambdas, options).Delegate;
        }

        public static void CompileToMethod(LambdaExpression lambda, MethodBuilder method, CompilerOptions options)
        {
            CompileToMethodInternal(lambda, method, null, options);
        }

        public static void CompileToMethod(LambdaExpression lambda, MethodBuilder method, DebugInfoGenerator debugInfoGenerator, CompilerOptions options)
        {
            CompileToMethodInternal(lambda, method, debugInfoGenerator, options);
        }

        public static bool AnalyzeILStack = true;

        internal static CompiledLambda CompileInternal(
            LambdaExpression lambda,
            DebugInfoGenerator debugInfoGenerator,
            Type closureType,
            ParameterExpression closureParameter,
            Type constantsType,
            ParameterExpression constantsParameter,
            object constants,
            Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches,
            CompilerOptions options,
            List<CompiledLambda> compiledLambdas)
        {
            var parameters = lambda.Parameters.ToArray();
            Type[] parameterTypes = parameters.Select(parameter => parameter.Type).ToArray();
            Type returnType = lambda.ReturnType;
            var method = new DynamicMethod(lambda.Name ?? Guid.NewGuid().ToString(), MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, returnType, parameterTypes, Module, true);
            using(var il = new GroboIL(method, AnalyzeILStack))
            {

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
                        ConstantsType = constantsType,
                        ConstantsParameter = constantsParameter,
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
                            il.Ldnull();
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
                        Delegate = method.CreateDelegate(Extensions.GetDelegateType(constantsParameter == null ? parameterTypes : parameterTypes.Skip(1).ToArray(), returnType), constants),
                        Method = method,
                        ILCode = il.GetILCode()
                    };
            }
        }

        internal static string CompileInternal(
            LambdaExpression lambda,
            DebugInfoGenerator debugInfoGenerator,
            Type closureType,
            ParameterExpression closureParameter,
            Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches,
            CompilerOptions options,
            List<CompiledLambda> compiledLambdas,
            MethodBuilder method)
        {
            var typeBuilder = method.ReflectedType as TypeBuilder;
            if(typeBuilder == null)
                throw new ArgumentException("Unable to obtain type builder of the method", "method");
            Type returnType = lambda.ReturnType;
            using(var il = new GroboIL(method, AnalyzeILStack))
            {

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
                            il.Ldnull();
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
        }

        internal static readonly AssemblyBuilder Assembly = CreateAssembly();
        internal static readonly ModuleBuilder Module = Assembly.DefineDynamicModule(Guid.NewGuid().ToString());

        private static AssemblyBuilder CreateAssembly()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
//            Type daType = typeof(AssemblyFlagsAttribute);
//            ConstructorInfo daCtor = daType.GetConstructor(new[] {typeof(AssemblyNameFlags)});
////[assembly : AssemblyFlags(AssemblyNameFlags.EnableJITcompileOptimizer)]
//            var daBuilder = new CustomAttributeBuilder(daCtor, new object[]
//                {
//                    AssemblyNameFlags.EnableJITcompileOptimizer
//                });
//            assemblyBuilder.SetCustomAttribute(daBuilder);
            return assemblyBuilder;
        }

        private static CompiledLambda CompileInternal(LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, out CompiledLambda[] subLambdas, CompilerOptions options)
        {
            var compiledLambdas = new List<CompiledLambda>();
            Type closureType;
            ParameterExpression closureParameter;
            Type constantsType;
            ParameterExpression constantsParameter;
            object constants;
            Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches;
            var resolvedLambda = new ExpressionClosureResolver(lambda, Module, true).Resolve(out closureType, out closureParameter, out constantsType, out constantsParameter, out constants, out switches);
            //here
            using (var writer = new StreamWriter("log.txt", false, Encoding.UTF8))
                AdvancedDebugViewWriter.WriteTo(resolvedLambda, writer);
            //here
            var compiledLambda = CompileInternal(resolvedLambda, debugInfoGenerator, closureType, closureParameter, constantsType, constantsParameter, constants, switches, options, compiledLambdas);
            subLambdas = compiledLambdas.ToArray();
            if(compiledLambdas.Count > 0)
                BuildDelegatesFoister(constantsType)(constants, compiledLambdas.Select(compiledLambda1 => compiledLambda1.Delegate).ToArray());
            return compiledLambda;
        }

        private static void CompileToMethodInternal(LambdaExpression lambda, MethodBuilder method, DebugInfoGenerator debugInfoGenerator, CompilerOptions options)
        {
            var compiledLambdas = new List<CompiledLambda>();
            Type closureType;
            ParameterExpression closureParameter;
            Type constantsType;
            ParameterExpression constantsParameter;
            object constants;
            Dictionary<SwitchExpression, Tuple<FieldInfo, FieldInfo, int>> switches;
            var module = method.Module as ModuleBuilder;
            if(module == null)
                throw new ArgumentException("Unable to obtain module builder of the method", "method");
            method.SetReturnType(lambda.ReturnType);
            method.SetParameters(lambda.Parameters.Select(parameter => parameter.Type).ToArray());
            var resolvedLambda = new ExpressionClosureResolver(lambda, module, false).Resolve(out closureType, out closureParameter, out constantsType, out constantsParameter, out constants, out switches);
            if(constantsParameter != null)
                throw new InvalidOperationException("Non-trivial constants are not allowed for compilation to method");
            CompileInternal(resolvedLambda, debugInfoGenerator, closureType, closureParameter, switches, options, compiledLambdas, method);
        }

        private static Action<object, Delegate[]> BuildDelegatesFoister(Type type)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new[] {typeof(object), typeof(Delegate[])}, Module, true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Castclass(type);
                il.Ldarg(1);
                il.Stfld(type.GetField("delegates"));
                il.Ret();
            }
            return (Action<object, Delegate[]>)method.CreateDelegate(typeof(Action<object, Delegate[]>));
        }
    }
}