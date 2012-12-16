using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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

        private static readonly Hashtable types = new Hashtable();
        private static readonly object typesLock = new object();
    }
}