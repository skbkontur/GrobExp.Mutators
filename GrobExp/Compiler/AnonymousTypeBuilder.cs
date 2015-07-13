using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GrobExp.Compiler
{
    public class AnonymousTypeBuilder
    {
        internal static readonly Hashtable anonymousTypes = new Hashtable();
        internal static readonly object anonymousTypesLock = new object();

        public static Type CreateAnonymousType(Type[] types, string[] names)
        {
            return CreateAnonymousType(types, names, Module);
        }

        public static Type CreateAnonymousType(Type[] types, string[] names, ModuleBuilder module)
        {
            var array = new TypesWithNamesArray(types, names);
            var type = (Type)anonymousTypes[array];
            if(type == null)
            {
                lock(anonymousTypesLock)
                {
                    type = (Type)anonymousTypes[array];
                    if(type == null)
                    {
                        type = BuildType(types, names, module);
                        anonymousTypes[array] = type;
                    }
                }
            }
            return type;
        }

        public static bool IsDynamicAnonymousType(Type type)
        {
            return type.Name.StartsWith("<>f__DynamicAnonymousType_");
        }

        public static bool IsAnonymousType(Type type)
        {
            return type.Name.StartsWith("<>f__AnonymousType") && !type.IsArray;
        }

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        internal static readonly ModuleBuilder Module = assembly.DefineDynamicModule(Guid.NewGuid().ToString(), true);

        private static Type BuildType(Type[] types, string[] names)
        {
            return BuildType(types, names, Module);
        }

        private static Type BuildType(Type[] types, string[] names, ModuleBuilder module)
        {
            int length = types.Length;
            if(names.Length != length)
                throw new InvalidOperationException();
            var typeBuilder = module.DefineType("<>f__DynamicAnonymousType_" + Guid.NewGuid(), TypeAttributes.Public | TypeAttributes.Class);
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, types);
            var constructorIl = constructor.GetILGenerator();
            for(int i = 0; i < length; ++i)
            {
                var field = typeBuilder.DefineField(names[i] + "_" + Guid.NewGuid(), types[i], FieldAttributes.Private | FieldAttributes.InitOnly);
                var property = typeBuilder.DefineProperty(names[i], PropertyAttributes.None, types[i], Type.EmptyTypes);
                var setter = typeBuilder.DefineMethod(names[i] + "_setter" + Guid.NewGuid(), MethodAttributes.Public, CallingConventions.HasThis, typeof(void), new[] {types[i]});
                var il = setter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0); // stack: [this]
                il.Emit(OpCodes.Ldarg_1); // stack: [this, value]
                il.Emit(OpCodes.Stfld, field); // this.field = value
                il.Emit(OpCodes.Ret);
                property.SetSetMethod(setter);

                var getter = typeBuilder.DefineMethod(names[i] + "_getter" + Guid.NewGuid(), MethodAttributes.Public, CallingConventions.HasThis, types[i], Type.EmptyTypes);
                il = getter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0); // stack: [this]
                il.Emit(OpCodes.Ldfld, field); // stack: [this.field]
                il.Emit(OpCodes.Ret);
                property.SetGetMethod(getter);

                constructorIl.Emit(OpCodes.Ldarg_0); // stack: [this]
                constructorIl.Emit(OpCodes.Ldarg_S, i + 1); // stack: [this, arg_i]
                constructorIl.Emit(OpCodes.Call, setter); // this.setter(arg_i)
            }
            constructorIl.Emit(OpCodes.Ret);
            return typeBuilder.CreateType();
        }
        internal class TypesWithNamesArray
        {
            public TypesWithNamesArray(Type[] types, string[] names)
            {
                Types = types;
                Names = names;
            }

            public override int GetHashCode()
            {
                return Names.Aggregate(Types.Aggregate(0, (current, t) => current * 314159265 + t.GetHashCode()), (current, t) => current * 271828459 + t.GetHashCode());
            }

            public override bool Equals(object obj)
            {
                if (!(obj is TypesWithNamesArray))
                    return false;
                if (ReferenceEquals(this, obj)) return true;
                var other = (TypesWithNamesArray)obj;
                if (Types.Length != other.Types.Length || Names.Length != other.Names.Length)
                    return false;
                if (Types.Where((t, i) => t != other.Types[i]).Any())
                    return false;
                if (Names.Where((s, i) => s != other.Names[i]).Any())
                    return false;
                return true;
            }

            public Type[] Types { get; private set; }
            public string[] Names { get; private set; }
        }
    }
}