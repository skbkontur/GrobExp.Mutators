using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GroBuf;
using GroBuf.DataMembersExtracters;

namespace GrobExp.Mutators
{
    public class Batch : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            throw new InvalidOperationException();
        }

        public void Add(object dest, object source)
        {
            throw new InvalidOperationException();
        }

        public void Add(object dest)
        {
            throw new InvalidOperationException();
        }
    }

    public static class MutatorsHelperFunctions
    {
        public static T Each<T>(this IEnumerable<T> arr)
        {
            throw new InvalidOperationException("Method " + EachMethod + " cannot be invoked");
        }

        public static T Current<T>(this IEnumerable<T> arr)
        {
            throw new InvalidOperationException("Method " + CurrentMethod + " cannot be invoked");
        }

        public static int CurrentIndex<T>(this T o)
        {
            throw new InvalidOperationException("Method " + CurrentIndexMethod + " cannot be invoked");
        }

        public static T TemplateIndex<T>(this T[] arr)
        {
            throw new InvalidOperationException("Method " + TemplateIndexMethod + " cannot be invoked");
        }

        public static object TemplateIndex(this Array arr)
        {
            throw new InvalidOperationException("Method " + NonGenericTemplateIndexMethod + " cannot be invoked");
        }

        public static T NotNull<T>(this T value)
        {
            throw new InvalidOperationException("Method " + NotNullMethod + " cannot be invoked");
        }

        public static T Dynamic<T>(this T obj)
        {
            return obj;
        }

        public static T IfNotNull<T>(this T o)
        {
            throw new InvalidOperationException();
        }

        public static void ForEach<T>(IList<T> arr, Func<T, int, T> func)
        {
            if (arr == null) return;
            for (var index = 0; index < arr.Count; ++index)
                arr[index] = func(arr[index], index);
        }

        public static void ForEach<T>(IEnumerable<T> enumerable, Action<T, int> action)
        {
            if (enumerable == null) return;
            var index = 0;
            foreach (var item in enumerable)
                action(item, index++);
        }

        public static void ForEach<TSourceKey, TDestKey, TSourceValue, TDestValue>(
            IDictionary<TSourceKey, TSourceValue> source,
            IDictionary<TDestKey, TDestValue> dest,
            Func<TSourceKey, TDestKey> keySelector,
            Func<TSourceValue, TDestValue, TDestValue> valueSelector)
        {
            if (source == null) return;
            foreach (var pair in source)
            {
                var key = keySelector(pair.Key);
                TDestValue value;
                if (!dest.TryGetValue(key, out value))
                    dest.Add(key, value = default(TDestValue));
                dest[key] = valueSelector(pair.Value, value);
            }
        }

        public static string JoinIgnoreEmpty(this string[] strings, string separator)
        {
            return string.Join(separator, strings.Where(s => !string.IsNullOrEmpty(s)));
        }

        public static bool IsNullOrEmpty(IEnumerable enumerable)
        {
            if (enumerable == null)
                return true;
            var enumerator = enumerable.GetEnumerator();
            return !enumerator.MoveNext();
        }

        public static bool IsNullOrEmpty(IEnumerable<string> strings)
        {
            return strings == null || strings.All(string.IsNullOrEmpty);
        }

        public static TTo ChangeType<TFrom, TTo>(TFrom value)
        {
            return serializer.ChangeType<TFrom, TTo>(value);
        }

        public static bool IsEachMethod(this MethodInfo method)
        {
            return method.IsGenericMethod && method.GetGenericMethodDefinition() == EachMethod;
        }

        public static bool IsCurrentMethod(this MethodInfo method)
        {
            return method == NonGenericTemplateIndexMethod || (method.IsGenericMethod && method.GetGenericMethodDefinition() == CurrentMethod);
        }

        public static bool IsStringIsNullOrEmptyMethod(this MethodInfo method)
        {
            return method == StringIsNullOrEmptyMethod;
        }

        public static bool IsCurrentIndexMethod(this MethodInfo method)
        {
            return method.IsGenericMethod && method.GetGenericMethodDefinition() == CurrentIndexMethod;
        }

        public static bool IsTemplateIndexMethod(this MethodInfo method)
        {
            return method == NonGenericTemplateIndexMethod || (method.IsGenericMethod && method.GetGenericMethodDefinition() == TemplateIndexMethod);
        }

        public static bool IsNotNullMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && methodInfo.GetGenericMethodDefinition() == NotNullMethod;
        }

        public static bool IsDynamicMethod(this MethodInfo method)
        {
            return method.IsGenericMethod && method.GetGenericMethodDefinition() == DynamicMethod;
        }

        public static bool IsWhereMethod(this MethodInfo method)
        {
            if (method.DeclaringType != typeof(Enumerable))
                return false;
            if (!method.IsGenericMethod)
                return false;
            return method.GetGenericMethodDefinition().Name == "Where";
        }

        public static bool IsToArrayMethod(this MethodInfo method)
        {
            if (method.DeclaringType != typeof(Enumerable))
                return false;
            if (!method.IsGenericMethod)
                return false;
            return method.GetGenericMethodDefinition().Name == "ToArray";
        }

        public static bool IsSelectMethod(this MethodInfo method)
        {
            if (method.DeclaringType != typeof(Enumerable))
                return false;
            if (!method.IsGenericMethod)
                return false;
            return method.GetGenericMethodDefinition().Name == "Select";
        }

        public static readonly MethodInfo DynamicMethod = ((MethodCallExpression)((Expression<Func<int, int>>)(i => i.Dynamic())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo EachMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.Each())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo CurrentMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.Current())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo CurrentIndexMethod = ((MethodCallExpression)((Expression<Func<int, int>>)(i => i.CurrentIndex())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo TemplateIndexMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.TemplateIndex())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo NonGenericTemplateIndexMethod = ((MethodCallExpression)((Expression<Func<Array, object>>)(arr => arr.TemplateIndex())).Body).Method;

        public static readonly MethodInfo NotNullMethod = ((MethodCallExpression)((Expression<Func<int, int>>)(i => i.NotNull())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo ArrayIsNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<int[], bool>>)(arr => IsNullOrEmpty(arr))).Body).Method;

        public static readonly MethodInfo StringArrayIsNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string[], bool>>)(strings => IsNullOrEmpty(strings))).Body).Method;

        public static readonly MethodInfo JoinIgnoreEmptyMethod = ((MethodCallExpression)((Expression<Func<string[], string>>)(arr => arr.JoinIgnoreEmpty(""))).Body).Method;

        public static readonly MethodInfo StringIsNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string, bool>>)(s => string.IsNullOrEmpty(s))).Body).Method;
        private static readonly ISerializer serializer = new Serializer(new AllPropertiesExtractor());
    }
}