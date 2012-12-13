using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace GrobExp
{
    public static class LambdaCompiler
    {
        public static Func<TResult> Compile<TResult>(Expression<Func<TResult>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var func = (Func<Delegate[], TResult>)Compile(lambda, options, out delegates);
            return () => func(delegates);
        }

        public static Action Compile(Expression<Action> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var action = (Action<Delegate[]>)Compile(lambda, options, out delegates);
            return () => action(delegates);
        }

        public static Func<T, TResult> Compile<T, TResult>(Expression<Func<T, TResult>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var func = (Func<Delegate[], T, TResult>)Compile(lambda, options, out delegates);
            return arg => func(delegates, arg);
        }

        public static Action<T> Compile<T>(Expression<Action<T>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var action = (Action<Delegate[], T>)Compile(lambda, options, out delegates);
            return arg => action(delegates, arg);
        }

        public static Func<T1, T2, TResult> Compile<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var func = (Func<Delegate[], T1, T2, TResult>)Compile(lambda, options, out delegates);
            return (arg1, arg2) => func(delegates, arg1, arg2);
        }

        public static Action<T1, T2> Compile<T1, T2>(Expression<Action<T1, T2>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var action = (Action<Delegate[], T1, T2>)Compile(lambda, options, out delegates);
            return (arg1, arg2) => action(delegates, arg1, arg2);
        }

        public static Func<T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var func = (Func<Delegate[], T1, T2, T3, TResult>)Compile(lambda, options, out delegates);
            return (arg1, arg2, arg3) => func(delegates, arg1, arg2, arg3);
        }

        public static Action<T1, T2, T3> Compile<T1, T2, T3>(Expression<Action<T1, T2, T3>> lambda, CompilerOptions options = CompilerOptions.All)
        {
            Delegate[] delegates;
            var action = (Action<Delegate[], T1, T2, T3>)Compile(lambda, options, out delegates);
            return (arg1, arg2, arg3) => action(delegates, arg1, arg2, arg3);
        }

        internal static CompiledLambda Compile(LambdaExpression lambda, CompilerOptions options, List<CompiledLambda> compiledLambdas)
        {
            return Compile(lambda, lambda.Parameters[0], options, compiledLambdas);
        }

        private static Delegate Compile(LambdaExpression lambda, CompilerOptions options, out Delegate[] delegates)
        {
            var compiledLambdas = new List<CompiledLambda>();
            ParameterExpression closureParameter;
            lambda = new ExpressionClosureResolver(lambda).Resolve(out closureParameter);
            var result = Compile(lambda, closureParameter, options, compiledLambdas);
            delegates = compiledLambdas.Select(compiledLambda => compiledLambda.Delegate).ToArray();
            var ilCode = new StringBuilder(result.ILCode);
            for(int i = 0; i < compiledLambdas.Count; ++i)
            {
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine();
                ilCode.AppendLine("delegates[" + i + "]");
                ilCode.AppendLine(compiledLambdas[i].ILCode);
            }
            return result.Delegate;
        }

        private static CompiledLambda Compile(LambdaExpression lambda, ParameterExpression closureParameter, CompilerOptions options, List<CompiledLambda> compiledLambdas)
        {
            var parameters = lambda.Parameters.ToArray();
            Type[] parameterTypes = parameters.Select(parameter => parameter.Type).ToArray();
            Type returnType = lambda.Body.Type;
            var method = new DynamicMethod(Guid.NewGuid().ToString(), returnType, parameterTypes, module, true);
            var il = new GrobIL(method.GetILGenerator(), false, returnType, parameterTypes);

            var context = new EmittingContext
                {
                    Options = options,
                    Parameters = parameters,
                    ClosureParameter = closureParameter,
                    CompiledLambdas = compiledLambdas,
                    Il = il
                };
            var returnDefaultValueLabel = il.DefineLabel("returnDefaultValue");
            Type resultType;
            bool labelUsed = ExpressionEmittersCollection.Emit(lambda.Body, context, returnDefaultValueLabel, out resultType);
            if(lambda.Body.Type == typeof(bool) && resultType == typeof(bool?))
                context.ConvertFromNullableBoolToBool();
            il.Ret();
            if(labelUsed && context.CanReturn)
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
                    Delegate = method.CreateDelegate(Extensions.GetDelegateType(parameterTypes, returnType)),
                    ILCode = il.GetILCode()
                };
        }

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());

        private enum ResultType
        {
            Value,
            ByRefAll,
            ByRefValueTypesOnly,
        }
    }
}