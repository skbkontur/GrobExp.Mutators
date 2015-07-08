using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrEmit;

namespace GrobExp.Compiler.ExpressionEmitters
{
    internal static class DynamicMethodInvokerBuilder
    {
        public static Type BuildDynamicMethodInvoker(Type[] constantTypes, Type resultType, Type[] parameterTypes)
        {
            string key = GetKey(constantTypes, resultType, parameterTypes);
            var type = (Type)types[key];
            if(type == null)
            {
                lock(typesLock)
                {
                    type = (Type)types[key];
                    if(type == null)
                    {
                        type = BuildDynamicMethodInvoker(key, constantTypes.Length, parameterTypes.Length, resultType == typeof(void));
                        types[key] = type;
                    }
                }
            }
            if(!type.IsGenericType)
                return type;
            var genericArguments = new List<Type>();
            genericArguments.AddRange(constantTypes);
            genericArguments.AddRange(parameterTypes);
            if(resultType != typeof(void))
                genericArguments.Add(resultType);
            return type.MakeGenericType(genericArguments.ToArray());
        }

        public static readonly Func<DynamicMethod, IntPtr> DynamicMethodPointerExtractor = EmitDynamicMethodPointerExtractor();

        private static Type BuildDynamicMethodInvoker(string name, int numberOfConstants, int numberOfParameters, bool returnsVoid)
        {
            var typeBuilder = LambdaCompiler.Module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
            var names = new List<string>();
            for(int i = 0; i < numberOfConstants; ++i)
                names.Add("TConst" + (i + 1));
            for(int i = 0; i < numberOfParameters; ++i)
                names.Add("TParam" + (i + 1));
            if(!returnsVoid)
                names.Add("TResult");
            var genericParameters = typeBuilder.DefineGenericParameters(names.ToArray());
            var constantTypes = genericParameters.Take(numberOfConstants).Cast<Type>().ToArray();
            var parameterTypes = genericParameters.Skip(numberOfConstants).Take(numberOfParameters).Cast<Type>().ToArray();
            Type genericResultType = returnsVoid ? typeof(void) : genericParameters.Last();
            var methodField = typeBuilder.DefineField("method", typeof(IntPtr), FieldAttributes.Private | FieldAttributes.InitOnly);
            var constantFields = new List<FieldInfo>();
            for (int i = 0; i < numberOfConstants; ++i)
                constantFields.Add(typeBuilder.DefineField("const_" + (i + 1), constantTypes[i], FieldAttributes.Public));

            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, constantTypes.Concat(new[] {typeof(IntPtr)}).ToArray());
            using(var il = new GroboIL(constructor))
            {
                for(int i = 0; i < numberOfConstants; ++i)
                {
                    il.Ldarg(0); // stack: [this]
                    il.Ldarg(i + 1); // stack: [this, arg_{i+1}]
                    il.Stfld(constantFields[i]); // this.const_{i+1} = arg_{i+1}; stack: []
                }
                il.Ldarg(0);
                il.Ldarg(numberOfConstants + 1);
                il.Stfld(methodField);
                il.Ret();
            }

            var method = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public, genericResultType, parameterTypes);
            using(var il = new GroboIL(method))
            {
                for (int i = 0; i < numberOfConstants; ++i)
                {
                    il.Ldarg(0); // stack: [this]
                    il.Ldfld(constantFields[i]); // stack: [this.const_{i+1}]
                }
                for(int i = 0; i < parameterTypes.Length; ++i)
                    il.Ldarg(i + 1);
                il.Ldarg(0);
                il.Ldfld(methodField);
                il.Calli(CallingConventions.Standard, genericResultType, genericParameters.Take(genericParameters.Length - 1).Cast<Type>().ToArray());
                il.Ret();
            }

            return typeBuilder.CreateType();
        }

        private static string GetKey(Type[] constantTypes, Type resultType, Type[] parameterTypes)
        {
            return resultType == typeof(void)
                ? string.Format("ActionInvoker_{0}_{1}", constantTypes.Length, parameterTypes.Length)
                : string.Format("FuncInvoker_{0}_{1}", constantTypes.Length, parameterTypes.Length);
        }

        private static Func<DynamicMethod, IntPtr> EmitDynamicMethodPointerExtractor()
        {
            var method = new DynamicMethod("DynamicMethodPointerExtractor", typeof(IntPtr), new[] {typeof(DynamicMethod)}, typeof(LambdaExpressionEmitter).Module, true);
            using(var il = new GroboIL(method))
            {
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
            }
            return (Func<DynamicMethod, IntPtr>)method.CreateDelegate(typeof(Func<DynamicMethod, IntPtr>));
        }

        private static readonly MethodInfo gcKeepAliveMethod = ((MethodCallExpression)((Expression<Action>)(() => GC.KeepAlive(null))).Body).Method;

        private static readonly Hashtable types = new Hashtable();
        private static readonly object typesLock = new object();
    }
}