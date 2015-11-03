using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class CanonicalFormsCache
    {
        public static Expression GetCanonicalForm(Expression validator)
        {
            if (cache == null)
                cache = new Hashtable();
            var form = new ExpressionCanonicalForm(validator);
            var key = new ExpressionWrapper(form.CanonicalForm, false);
            if (!cache.ContainsKey(key))
            {
                cache[key] = form.Lambda;
            }
            var lambda = (LambdaExpression)cache[key];
            return form.ConstructInvokation(lambda);
        }

        public static int Count()
        {
            return cache == null ? 0 : cache.Count;
        }

        [ThreadStatic]
        private static Hashtable cache;
    }
}
