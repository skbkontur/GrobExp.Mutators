using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrEmit;

using Sigil.NonGeneric;

namespace GrobExp.Compiler.ExpressionEmitters
{
    internal static class DynamicMethodInvokerBuilder
    {
        public static Type BuildDynamicMethodInvoker(Type constantsType, Type closureType, Type resultType, Type[] parameterTypes)
        {
            string key = GetKey(resultType, parameterTypes);
            var type = (Type)typesWithClosure[key];
            if(type == null)
            {
                lock(typesWithClosureLock)
                {
                    type = (Type)typesWithClosure[key];
                    if(type == null)
                    {
                        type = BuildDynamicMethodInvokerWithClosure(key, parameterTypes.Length, resultType == typeof(void));
                        typesWithClosure[key] = type;
                    }
                }
            }
            if(!type.IsGenericType)
                return type;
            var genericArguments = new List<Type> {constantsType, closureType};
            genericArguments.AddRange(parameterTypes);
            if(resultType != typeof(void))
                genericArguments.Add(resultType);
            return type.MakeGenericType(genericArguments.ToArray());
        }

        public static Type BuildDynamicMethodInvoker(Type constantsType, Type resultType, Type[] parameterTypes)
        {
            string key = GetKey(resultType, parameterTypes);
            var type = (Type)typesWithoutClosure[key];
            if(type == null)
            {
                lock(typesWithoutClosureLock)
                {
                    type = (Type)typesWithoutClosure[key];
                    if(type == null)
                    {
                        type = BuildDynamicMethodInvokerWithoutClosure(key, parameterTypes.Length, resultType == typeof(void));
                        typesWithoutClosure[key] = type;
                    }
                }
            }
            if(!type.IsGenericType)
                return type;
            var genericArguments = new List<Type> {constantsType};
            genericArguments.AddRange(parameterTypes);
            if(resultType != typeof(void))
                genericArguments.Add(resultType);
            return type.MakeGenericType(genericArguments.ToArray());
        }

        public static readonly Func<DynamicMethod, IntPtr> DynamicMethodPointerExtractor = EmitDynamicMethodPointerExtractor();

        private static Type BuildDynamicMethodInvokerWithClosure(string name, int numberOfParameters, bool returnsVoid)
        {
            var typeBuilder = LambdaCompiler.Module.DefineType(name + "_WithClosure", TypeAttributes.Public | TypeAttributes.Class);
            var names = new List<string> {"TConstants", "TClosure"};
            for(int i = 0; i < numberOfParameters; ++i)
                names.Add("T" + (i + 1));
            if(!returnsVoid)
                names.Add("TResult");
            GenericTypeParameterBuilder[] genericTypeParameters = typeBuilder.DefineGenericParameters(names.ToArray());
            Type genericConstantsType = genericTypeParameters.First();
            Type genericClosureType = genericTypeParameters.Skip(1).First();
            Type genericResultType = returnsVoid ? typeof(void) : genericTypeParameters.Last();
            Type[] genericParameterTypes = genericTypeParameters.Skip(2).Take(numberOfParameters).ToArray();
            var constantsField = typeBuilder.DefineField("constants", genericConstantsType, FieldAttributes.Private | FieldAttributes.InitOnly);
            var closureField = typeBuilder.DefineField("closure", genericClosureType, FieldAttributes.Private | FieldAttributes.InitOnly);
            var methodField = typeBuilder.DefineField("method", typeof(IntPtr), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] {genericConstantsType, genericClosureType, typeof(IntPtr)});
            {
                var il = constructor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, constantsField);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, closureField);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Stfld, methodField);
                il.Emit(OpCodes.Ret);
            }

            var method = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public, genericResultType, genericParameterTypes);
            {
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, constantsField);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, closureField);
                for(int i = 0; i < genericParameterTypes.Length; ++i)
                    il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, methodField);
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, genericResultType, new[] {genericConstantsType, genericClosureType}.Concat(genericParameterTypes).ToArray(), null);
                il.Emit(OpCodes.Ret);
            }

            return typeBuilder.CreateType();
        }

        private static Type BuildDynamicMethodInvokerWithoutClosure(string name, int numberOfParameters, bool returnsVoid)
        {
            var typeBuilder = LambdaCompiler.Module.DefineType(name + "_WithoutClosure", TypeAttributes.Public | TypeAttributes.Class);
            var names = new List<string> {"TConstants"};
            for(int i = 0; i < numberOfParameters; ++i)
                names.Add("T" + (i + 1));
            if(!returnsVoid)
                names.Add("TResult");
            GenericTypeParameterBuilder[] genericTypeParameters = typeBuilder.DefineGenericParameters(names.ToArray());
            Type genericConstantsType = genericTypeParameters.First();
            Type genericResultType = returnsVoid ? typeof(void) : genericTypeParameters.Last();
            Type[] genericParameterTypes = genericTypeParameters.Skip(1).Take(numberOfParameters).ToArray();
            var constantsField = typeBuilder.DefineField("constants", genericConstantsType, FieldAttributes.Private | FieldAttributes.InitOnly);
            var methodField = typeBuilder.DefineField("method", typeof(IntPtr), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] {genericConstantsType, typeof(IntPtr)});
            {
                var il = constructor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, constantsField);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, methodField);
                il.Emit(OpCodes.Ret);
            }

            var method = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public, genericResultType, genericParameterTypes);
            {
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, constantsField);
                for(int i = 0; i < genericParameterTypes.Length; ++i)
                    il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, methodField);
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, genericResultType, new[] {genericConstantsType}.Concat(genericParameterTypes).ToArray(), null);
                il.Emit(OpCodes.Ret);
            }

            return typeBuilder.CreateType();
        }

        private static string GetKey(Type resultType, Type[] parameterTypes)
        {
            return resultType == typeof(void) ? "ActionInvoker_" + parameterTypes.Length : "FuncInvoker_" + parameterTypes.Length;
        }

        private static Func<DynamicMethod, IntPtr> EmitDynamicMethodPointerExtractor()
        {
            //var method = new DynamicMethod("DynamicMethodPointerExtractor", typeof(IntPtr), new[] {typeof(DynamicMethod)}, typeof(LambdaExpressionEmitter).Module, true);
            var emit = Emit.NewDynamicMethod(typeof(IntPtr), new[] {typeof(DynamicMethod)});
            var il = new GroboIL(emit.AsShorthand());
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
            return (Func<DynamicMethod, IntPtr>)emit.CreateDelegate(typeof(Func<DynamicMethod, IntPtr>));
        }

        private static readonly MethodInfo gcKeepAliveMethod = ((MethodCallExpression)((Expression<Action>)(() => GC.KeepAlive(null))).Body).Method;

        private static readonly Hashtable typesWithClosure = new Hashtable();
        private static readonly object typesWithClosureLock = new object();
        private static readonly Hashtable typesWithoutClosure = new Hashtable();
        private static readonly object typesWithoutClosureLock = new object();
    }
}