using System;
using System.Linq;

namespace GrobExp
{
    internal static class Extensions
    {
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsStruct(this Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        public static Type GetDelegateType(Type[] parameterTypes, Type returnType)
        {
            if (returnType == typeof(void))
            {
                switch (parameterTypes.Length)
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
                    default:
                        throw new NotSupportedException("Too many parameters for Action: " + parameterTypes.Length);
                }
            }
            parameterTypes = parameterTypes.Concat(new[] { returnType }).ToArray();
            switch (parameterTypes.Length)
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