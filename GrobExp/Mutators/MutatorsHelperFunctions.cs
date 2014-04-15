using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
            if(arr == null) return;
            for(int index = 0; index < arr.Count; ++index)
                arr[index] = func(arr[index], index);
        }

        public static void ForEach<T>(IEnumerable<T> enumerable, Action<T, int> action)
        {
            if(enumerable == null) return;
            int index = 0;
            foreach(var item in enumerable)
                action(item, index++);
        }

        public static string JoinIgnoreEmpty(this string[] strings, string separator)
        {
            return string.Join(separator, strings.Where(s => !string.IsNullOrEmpty(s)));
        }

        public static bool IsNullOrEmpty(Array array)
        {
            return array == null || array.Length == 0;
        }

        public static bool IsNullOrEmpty(string[] strings)
        {
            return strings == null || strings.Length == 0 || strings.All(string.IsNullOrEmpty);
        }

        public static bool IsEachMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && methodInfo.GetGenericMethodDefinition() == EachMethod;
        }

        public static bool IsCurrentMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && methodInfo.GetGenericMethodDefinition() == CurrentMethod;
        }

        public static bool IsStringIsNullOrEmptyMethod(this MethodInfo methodInfo)
        {
            return methodInfo == StringIsNullOrEmptyMethod;
        }

        public static bool IsCurrentIndexMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && methodInfo.GetGenericMethodDefinition() == CurrentIndexMethod;
        }

        public static bool IsTemplateIndexMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && methodInfo.GetGenericMethodDefinition() == TemplateIndexMethod;
        }

        public static bool IsNotNullMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && methodInfo.GetGenericMethodDefinition() == NotNullMethod;
        }

        public static bool IsDynamicMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && methodInfo.GetGenericMethodDefinition() == DynamicMethod;
        }

        public static bool IsWhereMethod(this MethodInfo methodInfo)
        {
            if(methodInfo.DeclaringType != typeof(Enumerable))
                return false;
            if(!methodInfo.IsGenericMethod)
                return false;
            return methodInfo.GetGenericMethodDefinition().Name == "Where";
        }

        public static bool IsToArrayMethod(this MethodInfo methodInfo)
        {
            if(methodInfo.DeclaringType != typeof(Enumerable))
                return false;
            if(!methodInfo.IsGenericMethod)
                return false;
            return methodInfo.GetGenericMethodDefinition().Name == "ToArray";
        }

        public static bool IsSelectMethod(this MethodInfo methodInfo)
        {
            if(methodInfo.DeclaringType != typeof(Enumerable))
                return false;
            if(!methodInfo.IsGenericMethod)
                return false;
            return methodInfo.GetGenericMethodDefinition().Name == "Select";
        }

        public static readonly MethodInfo DynamicMethod = ((MethodCallExpression)((Expression<Func<int, int>>)(i => i.Dynamic())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo EachMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.Each())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo CurrentMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.Current())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo CurrentIndexMethod = ((MethodCallExpression)((Expression<Func<int, int>>)(i => i.CurrentIndex())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo TemplateIndexMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.TemplateIndex())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo NotNullMethod = ((MethodCallExpression)((Expression<Func<int, int>>)(i => i.NotNull())).Body).Method.GetGenericMethodDefinition();

        public static readonly MethodInfo ArrayIsNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<int[], bool>>)(arr => IsNullOrEmpty(arr))).Body).Method;

        public static readonly MethodInfo StringArrayIsNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string[], bool>>)(strings => IsNullOrEmpty(strings))).Body).Method;

        public static readonly MethodInfo JoinIgnoreEmptyMethod = ((MethodCallExpression)((Expression<Func<string[], string>>)(arr => arr.JoinIgnoreEmpty(""))).Body).Method;

        public static readonly MethodInfo StringIsNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string, bool>>)(s => string.IsNullOrEmpty(s))).Body).Method;
    }
}