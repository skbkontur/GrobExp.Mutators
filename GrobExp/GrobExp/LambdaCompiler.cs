using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using GrEmit;

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
            var result = Compile(resolvedLambda, closureType, closureParameter, options, compiledLambdas);
            if(compiledLambdas.Count > 0)
            {
                var delegatesFoister = BuildDelegatesFoister(closureType);
                delegatesFoister(compiledLambdas.Select(compiledLambda => compiledLambda.Delegate).ToArray());
            }
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
            return result.Delegate as TDelegate;
        }

        public static readonly AssemblyBuilder Assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        public static readonly ModuleBuilder Module = Assembly.DefineDynamicModule(Guid.NewGuid().ToString());

        internal static CompiledLambda Compile(LambdaExpression lambda, Type closureType, ParameterExpression closureParameter, CompilerOptions options, List<CompiledLambda> compiledLambdas)
        {
            var parameters = lambda.Parameters.ToArray();
            Type[] parameterTypes = parameters.Select(parameter => parameter.Type).ToArray();
            Type returnType = lambda.Body.Type;
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
                    Method = method,
                    ILCode = il.GetILCode()
                };
        }

        private static Action<Delegate[]> BuildDelegatesFoister(Type type)
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new[] {typeof(Delegate[])}, Module, true);
            var il = new GroboIL(method);
            il.Ldnull();
            il.Ldarg(0);
            il.Stfld(type.GetField("delegates", BindingFlags.Public | BindingFlags.Static));
            il.Ret();
            return (Action<Delegate[]>)method.CreateDelegate(typeof(Action<Delegate[]>));
        }

        private enum ResultType
        {
            Void,
            Value,
            ByRefAll,
            ByRefValueTypesOnly,
        }
    }
}