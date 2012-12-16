using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    public static class DynamicMethodInvokerBuilder
    {
        public static Type BuildDynamicMethodInvoker(Type closureType, Type resultType, Type[] parameterTypes)
        {
            string key = GetKey(resultType, parameterTypes);
            var type = (Type)types[key];
            if(type == null)
            {
                lock(typesLock)
                {
                    type = (Type)types[key];
                    if(type == null)
                    {
                        type = BuildDynamicMethodInvokerInternal(key, parameterTypes.Length, resultType == typeof(void));
                        types[key] = type;
                    }
                }
            }
            var genericArguments = new List<Type> {closureType};
            genericArguments.AddRange(parameterTypes);
            if(resultType != typeof(void))
                genericArguments.Add(resultType);
            return type.MakeGenericType(genericArguments.ToArray());
        }

        public static readonly Func<DynamicMethod, IntPtr> DynamicMethodPointerExtractor = EmitDynamicMethodPointerExtractor();

        private static Type BuildDynamicMethodInvokerInternal(string name, int numberOfParameters, bool returnsVoid)
        {
            var typeBuilder = LambdaCompiler.Module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
            var names = new List<string> {"TClosure"};
            for(int i = 0; i < numberOfParameters; ++i)
                names.Add("T" + (i + 1));
            if(!returnsVoid)
                names.Add("TResult");
            GenericTypeParameterBuilder[] genericTypeParameters = typeBuilder.DefineGenericParameters(names.ToArray());
            Type genericClosureType = genericTypeParameters.First();
            Type genericResultType = returnsVoid ? typeof(void) : genericTypeParameters.Last();
            Type[] genericParameterTypes = genericTypeParameters.Skip(1).Take(numberOfParameters).ToArray();
            var closureField = typeBuilder.DefineField("closure", genericClosureType, FieldAttributes.Private | FieldAttributes.InitOnly);
            var methodField = typeBuilder.DefineField("method", typeof(IntPtr), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] {genericClosureType, typeof(IntPtr)});
            var il = new GroboIL(constructor);
            il.Ldarg(0);
            il.Ldarg(1);
            il.Stfld(closureField);
            il.Ldarg(0);
            il.Ldarg(2);
            il.Stfld(methodField);
            il.Ret();

            var method = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public, genericResultType, genericParameterTypes);
            il = new GroboIL(method);
            il.Ldarg(0);
            il.Ldfld(closureField);
            for(int i = 0; i < genericParameterTypes.Length; ++i)
                il.Ldarg(i + 1);
            il.Ldarg(0);
            il.Ldfld(methodField);
            il.Calli(CallingConventions.Standard, genericResultType, new[] {genericClosureType}.Concat(genericParameterTypes).ToArray());
            il.Ret();

            return typeBuilder.CreateType();
        }

        private static string GetKey(Type resultType, Type[] parameterTypes)
        {
            return resultType == typeof(void) ? "ActionInvoker_" + parameterTypes.Length : "FuncInvoker_" + parameterTypes.Length;
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

        private static readonly Hashtable types = new Hashtable();
        private static readonly object typesLock = new object();
    }
}