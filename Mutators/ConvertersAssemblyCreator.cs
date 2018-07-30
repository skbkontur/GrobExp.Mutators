using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

using GrEmit;

namespace GrobExp.Mutators
{
    public class ConvertersAssemblyCreator : MarshalByRefObject
    {
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;
        private readonly List<TypeBuilder> typeBuilders;

        public ConvertersAssemblyCreator()
        {
            var assemblyName = "Converters";
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            typeBuilders = new List<TypeBuilder>();
        }

        public TypeBuilder CreateConverterType(Type converterType)
        {
            var converterTypeName = CreateConverterTypeName(converterType);
            var typeBuilder = moduleBuilder.DefineType(converterTypeName, TypeAttributes.Public | TypeAttributes.Class);
            typeBuilders.Add(typeBuilder);
            return typeBuilder;
        }

        public string CreateConverterTypeName(Type converterType)
        {
            var converterTypeName = converterType.Name;
            if (converterType.IsGenericType)
                converterTypeName = $"{converterTypeName.Substring(0, converterTypeName.Length - 2)}<{string.Join(" ", converterType.GenericTypeArguments.Select(x => x.Name))}>";
            return converterTypeName;
        }

        public void SaveAssembly(string assemblySavePath)
        {
            Environment.CurrentDirectory = assemblySavePath;
            foreach(var typeBuilder in typeBuilders)
                typeBuilder.CreateType();
            assemblyBuilder.Save($"{assemblyBuilder.GetName().Name}.dll");
        }
    }
}