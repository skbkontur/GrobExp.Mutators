using System;
using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Compiler
{
    public static class Extensions
    {
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsStruct(this Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        public static bool IsDictionary(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public static bool Unsigned(this Type type)
        {
            if(type == typeof(IntPtr))
                return false;
            if(type == typeof(UIntPtr))
                return true;
            switch(Type.GetTypeCode(type))
            {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.Char:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                return false;
            default:
                throw new InvalidOperationException(string.Format("Type '{0}' cannot be used in comparison operations", type));
            }
        }

        public static bool IsStronglyPublic(this Type type)
        {
            if (type == null || type.IsGenericParameter) return true;

            if (type.IsArray)
            {
                if (!type.IsNested)
                {
                    if (!type.IsPublic)
                        return false;
                }
                else if (!type.IsNestedPublic || !IsStronglyPublic(type.DeclaringType))
                    return false;
                var elem = type.GetElementType();
                return IsStronglyPublic(elem);
            }

            if (type.IsGenericType)
            {
                if (!type.IsNested)
                {
                    if (!type.IsPublic)
                        return false;
                }
                else if (!type.IsNestedPublic || !IsStronglyPublic(type.DeclaringType))
                    return false;
                var parameters = type.GetGenericArguments();
                return parameters.All(IsStronglyPublic);
            }

            if (!type.IsNested)
                return type.IsPublic;
            return type.IsNestedPublic && IsStronglyPublic(type.DeclaringType);
        }

        public static Type GetDelegateType(Type[] parameterTypes, Type returnType)
        {
            if(returnType == typeof(void))
            {
                switch(parameterTypes.Length)
                {
                case 0:
                    return typeof(Action);
                case 1:
                    return typeof(Action<>).MakeGenericType(parameterTypes);
                case 2:
                    return typeof(Action<,>).MakeGenericType(parameterTypes);
                case 3:
                    return typeof(Action<,,>).MakeGenericType(parameterTypes);
                case 4:
                    return typeof(Action<,,,>).MakeGenericType(parameterTypes);
                case 5:
                    return typeof(Action<,,,,>).MakeGenericType(parameterTypes);
                case 6:
                    return typeof(Action<,,,,,>).MakeGenericType(parameterTypes);
                case 7:
                    return typeof(Action<,,,,,,>).MakeGenericType(parameterTypes);
                case 8:
                    return typeof(Action<,,,,,,,>).MakeGenericType(parameterTypes);
                default:
                    throw new NotSupportedException("Too many parameters for Action: " + parameterTypes.Length);
                }
            }
            parameterTypes = parameterTypes.Concat(new[] {returnType}).ToArray();
            switch(parameterTypes.Length)
            {
            case 1:
                return typeof(Func<>).MakeGenericType(parameterTypes);
            case 2:
                return typeof(Func<,>).MakeGenericType(parameterTypes);
            case 3:
                return typeof(Func<,,>).MakeGenericType(parameterTypes);
            case 4:
                return typeof(Func<,,,>).MakeGenericType(parameterTypes);
            case 5:
                return typeof(Func<,,,,>).MakeGenericType(parameterTypes);
            case 6:
                return typeof(Func<,,,,,>).MakeGenericType(parameterTypes);
            default:
                throw new NotSupportedException("Too many parameters for Func: " + parameterTypes.Length);
            }
        }
    }
}