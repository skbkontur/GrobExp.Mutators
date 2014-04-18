using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators
{
    public static class TypeExtensions
    {
        public static object GetDefaultValue(this Type type)
        {
            object result = defaultValues[type];
            if(result == null)
            {
                lock(defaultValuesLock)
                {
                    result = defaultValues[type];
                    if(result == null)
                    {
                        result = getDefaultValueMethod.MakeGenericMethod(type).Invoke(null, null) ?? nullDefault;
                        defaultValues[type] = result;
                    }
                }
            }
            return result == nullDefault ? null : result;
        }

        public static Type GetItemType(this Type type)
        {
            if(type.IsArray) return type.GetElementType();
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments().Single();
            foreach(var t in type.GetInterfaces())
            {
                if(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return t.GetGenericArguments().Single();
            }
            throw new ArgumentException("Unable to extract item type of '" + type + "'");
        }

        public static bool IsAnonymousType(this Type type)
        {
            return type.Name.StartsWith("<>f__AnonymousType");
        }

        private static T Default<T>()
        {
            return default(T);
        }

        private static readonly MethodInfo getDefaultValueMethod =
            ((MethodCallExpression)((Expression<Func<object, object>>)(o => Default<object>())).Body).Method.GetGenericMethodDefinition();

        private static readonly Hashtable defaultValues = new Hashtable();
        private static readonly object defaultValuesLock = new object();

        private static readonly object nullDefault = new object();
    }
}