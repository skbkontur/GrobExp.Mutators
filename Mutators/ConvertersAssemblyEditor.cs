using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

using GrEmit;

namespace GrobExp.Mutators
{
    public class ConvertersAssemblyEditor : MarshalByRefObject
    {
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;
        private readonly List<TypeBuilder> typeBuilders;

        public ConvertersAssemblyEditor()
        {
            var assemblyName = "Converters";
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            typeBuilders = new List<TypeBuilder>();
        }

        public TypeBuilder CreateConverterType(string converterTypeName)
        {
            var typeBuilder = moduleBuilder.DefineType(converterTypeName, TypeAttributes.Public | TypeAttributes.Class);
            typeBuilders.Add(typeBuilder);
            return typeBuilder;
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