using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class CompiledExpressionsCache
    {
        public static Expression GetCanonicalForm(Expression expression)
        {
            var form = new ExpressionCanonicalForm(expression);
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

        public static Expression GetCachedExpression(Expression validator)
        {
            var key = new ExpressionWrapper(validator, false);
            var exp = (Action<object[]>)outerCache[key];
            if(exp == null)
            {
                lock(lockObject2)
                {
                    exp = (Action<object[]>)outerCache[key];
                    if(exp == null)
                    {
                        outerCache[key] = exp = new MethodExtractor(validator).Method;
                    }
                }
            }
            var arguments = validator.ExtractParameters().Select(e => Expression.Convert(e, typeof(object)));
            return Expression.Invoke(Expression.Constant(exp), 
                Expression.NewArrayInit(typeof(object), arguments));
        }

        public static int FormsCount()
        {
            return cache.Count;
        }

        public static int Count()
        {
            return outerCache.Count;
        }

        private static readonly Hashtable cache = new Hashtable();
        private static readonly Hashtable outerCache = new Hashtable();
        private static readonly object lockObject = new object(), lockObject2 = new object();

        public static void Clear()
        {
            lock(lockObject)
            {
                cache.Clear();
            }
            lock(lockObject2)
            {
                outerCache.Clear();
            }
        }
    }
}
