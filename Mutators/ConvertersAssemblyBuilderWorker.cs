using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

using GrEmit;

namespace GrobExp.Mutators
{
    public class ConvertersAssemblyBuilderWorker
    {
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;
        private readonly List<TypeBuilder> typeBuilders;

        public ConvertersAssemblyBuilderWorker()
        {
            var assemblyName = "Converters";
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            typeBuilders = new List<TypeBuilder>();
        }

        public TypeBuilder CreateClass(string className)
        {
            var typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class);
            typeBuilders.Add(typeBuilder);
            return typeBuilder;
        }

        public TypeBuilder CreateConverterClass(Type converterClass)
        {
            var converterClassName = CreateConverterTypeName(converterClass);
            var typeBuilder = moduleBuilder.DefineType(converterClassName, TypeAttributes.Public | TypeAttributes.Class);
            typeBuilders.Add(typeBuilder);
            return typeBuilder;
        }

        public static string CreateConverterTypeName(Type converterType)
        {
            var converterTypeName = converterType.Name;
            if (converterType.IsGenericType)
                converterTypeName = $"{converterTypeName.Substring(0, converterTypeName.Length - 2)}<{string.Join(" ", converterType.GenericTypeArguments.Select(x => x.Name))}>";
            return converterTypeName;
        }

        public void SaveAssembly()
        {
            foreach(var typeBuilder in typeBuilders)
                typeBuilder.CreateType();
            assemblyBuilder.Save($"{assemblyBuilder.GetName().Name}.dll");
        }
    }
}