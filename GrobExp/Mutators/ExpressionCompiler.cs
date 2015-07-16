using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrobExp.Compiler;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class ExpressionCompiler
    {
        public static Func<TResult> Compile<TResult>(Expression<Func<TResult>> exp, bool forceDynamic = false)
        {
            object[] constants;
            var func = (Func<object[], TResult>)Compile(KillConstants(exp, out constants), forceDynamic);
            return () => func(constants);
        }

        public static Func<T, TResult> Compile<T, TResult>(Expression<Func<T, TResult>> exp, bool forceDynamic = false)
        {
            object[] constants;
            var func = (Func<T, object[], TResult>)Compile(KillConstants(exp, out constants), forceDynamic);
            return arg => func(arg, constants);
        }

        public static Func<T1, T2, TResult> Compile<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> exp, bool forceDynamic = false)
        {
            object[] constants;
            var func = (Func<T1, T2, object[], TResult>)Compile(KillConstants(exp, out constants), forceDynamic);
            return (arg1, arg2) => func(arg1, arg2, constants);
        }

        public static readonly Func<Expression, string> DebugViewGetter = BuildDebugViewGetter();

        private static LambdaExpression KillConstants(LambdaExpression lambda, out object[] constants)
        {
            var constantsParameter = Expression.Parameter(typeof(object[]), "C0NSTS");
            var bodyWithoutConstants = new ExpressionConstantsExtractor(constantsParameter).ExtractConstants(lambda.Body, out constants);
            return Expression.Lambda(bodyWithoutConstants, lambda.Parameters.Concat(new[] {constantsParameter}));
        }

        private static Delegate Compile(LambdaExpression lambda, bool forceDynamic)
        {
            //var key = new ExpressionWrapper(lambda);
            var key = forceDynamic + DebugViewGetter(lambda);
            var result = hashtable[key];
            if(result == null)
            {
                lock(lockObject)
                {
                    result = hashtable[key];
                    if(result == null)
                    {
                        result = LambdaCompiler.Compile(lambda, CompilerOptions.All, forceDynamic); //lambda.Compile();
                        hashtable[key] = result;
                    }
                }
            }
            return (Delegate)result;
        }

        private static Func<Expression, string> BuildDebugViewGetter()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(string), new[] {typeof(Expression)}, typeof(ExpressionCompiler), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [exp]
            var debugViewProperty = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            if(debugViewProperty == null)
                throw new MissingMemberException("Expression.DebugView");
            var getter = debugViewProperty.GetGetMethod(true);
            if(getter == null)
                throw new MissingMethodException("Expression.DebugView_get");
            il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter); // stack: [exp.DebugView]
            il.Emit(OpCodes.Ret);
            return (Func<Expression, string>)method.CreateDelegate(typeof(Func<Expression, string>));
        }

        private static readonly Hashtable hashtable = new Hashtable();
        private static readonly object lockObject = new object();
    }
}