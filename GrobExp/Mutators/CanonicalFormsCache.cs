using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class CanonicalFormsCache
    {
        public static Expression GetCanonicalForm(Expression validator)
        {
            var form = new ExpressionCanonicalForm(validator);
            var key = new ExpressionWrapper(form.CanonicalForm, false);
            var lambda = (Action<object[]>)cache[key];
            if(lambda == null)
            {
                lock(lockObject)
                {
                    lambda = (Action<object[]>)cache[key];
                    if(lambda == null)
                    {
                        cache[key] = lambda = (Action<object[]>)LambdaCompiler.Compile(form.Lambda, CompilerOptions.All);
                    }
                }
            }
            return form.ConstructInvokation(lambda);
        }

        public static int Count()
        {
            return cache == null ? 0 : cache.Count;
        }

        private static readonly Hashtable cache = new Hashtable();
        private static readonly object lockObject = new object();

        public static void Clear()
        {
            lock(lockObject)
            {
                cache.Clear();
            }
        }
    }
}
