using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class CanonicalFormsCache
    {
        public static Expression GetCanonicalForm(Expression validator, params ParameterExpression[] parametersToExtract)
        {
            if (cache == null)
                cache = new Dictionary<ExpressionWrapper, LambdaExpression>();
            var form = new ExpressionCanonicalForm(validator, parametersToExtract);
            var key = new ExpressionWrapper(form.CanonicalForm, true);
            LambdaExpression lambda;
            if (!cache.TryGetValue(key, out lambda))
            {
                cache[key] = lambda = Expression.Lambda(form.CanonicalForm, form.ParameterAccessor);
            }
            return form.ConstructInvokation(lambda);
        }

        [ThreadStatic]
        private static Dictionary<ExpressionWrapper, LambdaExpression> cache;
    }
}
