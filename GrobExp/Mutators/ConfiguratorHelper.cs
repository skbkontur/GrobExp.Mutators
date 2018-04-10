using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators
{
    public static class ConfiguratorHelper
    {
        public static bool IsLeafType(Type type)
        {
            return type == typeof(object) || type.IsPrimitive || type.IsEnum || type.IsInterface || type == typeof(string)
                   || type == typeof(DateTime) || type == typeof(decimal)
                   || type.IsArray && IsLeafType(type.GetElementType())
                   || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                   || type == typeof(MultiLanguageTextBase) || type.IsSubclassOf(typeof(MultiLanguageTextBase));
        }

        public static LambdaExpression[] GetLeaves(Type type)
        {
            var result = new List<Expression>();
            var parameter = Expression.Parameter(type);
            GetLeaves(type, parameter, result);
            return result.Select(path => Expression.Lambda((Expression)path, parameter)).ToArray();
        }

        private static void GetLeaves(Type type, Expression path, List<Expression> result)
        {
            if (IsLeafType(type))
            {
                result.Add(path);
                return;
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                GetLeaves(elementType, Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(elementType), path), result);
                return;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Func<PropertyInfo, bool> predicate = property => property.CanWrite && property.GetSetMethod(true).GetParameters().Length == 1;

            if (!properties.All(predicate) && type.GetConstructor(Type.EmptyTypes) == null)
            {
                //throw new InvalidOperationException(string.Format("Unable to slice into pieces compex type '{0}' because it has no parameterless constructor and has at least one property with no setter", type));
                // Consider it a leaf
                result.Add(path);
                return;
            }

            foreach (var property in properties.Where(predicate))
                GetLeaves(property.PropertyType, Expression.Property(path, property), result);
        }
    }
}