using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GrobExp.Mutators
{
    public static class MethodInfoExtensions
    {
        public static bool IsExtension(this MethodInfo method)
        {
            return method.GetCustomAttributes(typeof(ExtensionAttribute), false).Any();
        }

        public static bool IsIndexerGetter(this MethodInfo method)
        {
            return method.Name == "get_Item";
        }

        public static bool IsArrayIndexer(this MethodInfo method)
        {
            return method == arrayGetValueMethod;
        }

        private static readonly MethodInfo arrayGetValueMethod = ((MethodCallExpression)((Expression<Func<Array, object>>)(arr => arr.GetValue(0))).Body).Method;
    }
}