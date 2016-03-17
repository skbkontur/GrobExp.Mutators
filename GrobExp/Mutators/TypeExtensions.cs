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
            var result = defaultValues[type];
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

        public static Type TryGetItemType(this Type type)
        {
            if(type.IsArray) return type.GetElementType();
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments().Single();
            foreach(var t in type.GetInterfaces())
            {
                if(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return t.GetGenericArguments().Single();
            }
            if(type == typeof(IEnumerable))
                return typeof(object);
            if(type.GetInterfaces().Any(t => t == typeof(IEnumerable)))
                return typeof(object);
            return null;
        }

        public static Type GetItemType(this Type type)
        {
            var result = type.TryGetItemType();
            if(result == null)
                throw new ArgumentException("Unable to extract item type of '" + type + "'");
            return result;
        }

        public static bool IsAnonymousType(this Type type)
        {
            return type.Name.StartsWith("<>f__AnonymousType");
        }

        public static bool IsTuple(this Type type)
        {
            if(!type.IsGenericType) return false;
            type = type.GetGenericTypeDefinition();
            return type == typeof(Tuple<>)
                   || type == typeof(Tuple<,>)
                   || type == typeof(Tuple<,,>)
                   || type == typeof(Tuple<,,,>)
                   || type == typeof(Tuple<,,,,>)
                   || type == typeof(Tuple<,,,,,>)
                   || type == typeof(Tuple<,,,,,,>);
        }

        public static PropertyInfo[] GetOrderedProperties(this Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            var result = new List<PropertyInfo>();
            GetProperties(type, flags, result);
            result.Sort((first, second) =>
                {
                    if(first.Module.Assembly != second.Module.Assembly)
                        return string.Compare(first.Module.Assembly.FullName, second.Module.Assembly.FullName, StringComparison.InvariantCulture);
                    if(first.Module.MetadataToken != second.Module.MetadataToken)
                        return first.Module.MetadataToken - second.Module.MetadataToken;
                    return first.MetadataToken - second.MetadataToken;
                });
            var unique = new List<PropertyInfo>();
            if(result.Count > 0)
                unique.Add(result[0]);
            for(var i = 1; i < result.Count; ++i)
            {
                if(result[i - 1].Module != result[i].Module || result[i - 1].MetadataToken != result[i].MetadataToken)
                    unique.Add(result[i]);
            }
            return unique.ToArray();
        }

        private static void GetProperties(Type type, BindingFlags flags, List<PropertyInfo> list)
        {
            if(type == null || type == typeof(object))
                return;
            list.AddRange(type.GetProperties(flags | BindingFlags.DeclaredOnly));
            GetProperties(type.BaseType, flags, list);
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