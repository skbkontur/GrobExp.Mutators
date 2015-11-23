using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GrobExp.Mutators
{
    public static class ExpressionTypeBuilder
    {
        public static Type BuildType(Expression[] expressionsToExtract, string[] fieldNames)
        {
            typeBuilder = module.DefineType("Closure__" + id++, TypeAttributes.Class | TypeAttributes.Public);
            for (var i = 0; i < expressionsToExtract.Length; ++i)
            {
                typeBuilder.DefineField(fieldNames[i], expressionsToExtract[i].Type, FieldAttributes.Public);
            }
            return typeBuilder.CreateType();
        }

        public static string[] GenerateFieldNames(Expression[] extractedExpressions)
        {
            var indexes = new Dictionary<Type, int>();
            var result = new string[extractedExpressions.Length];
            for (var i = 0; i < extractedExpressions.Length; ++i)
            {
                var expressionType = extractedExpressions[i].Type;
                var index = indexes.ContainsKey(expressionType) ? indexes[expressionType] + 1 : 0;
                indexes[expressionType] = index;
                result[i] = Format(expressionType) + "_" + index;
            }
            return result;
        }

        public static FieldInfo[] GetFieldInfos(Type type, string[] fieldNames)
        {
            return fieldNames.Select(type.GetField).ToArray();
        }

        private static string Format(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            return type.Name + "<" + string.Join(", ", type.GetGenericArguments().Select(Format)) + ">";
        }

        private static int id = 0;
        private static TypeBuilder typeBuilder;

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndSave);
        private static readonly ModuleBuilder module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
    }
}
