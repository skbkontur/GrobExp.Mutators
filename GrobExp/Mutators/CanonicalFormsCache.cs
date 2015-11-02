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
            if (Cache == null)
                Cache = new Dictionary<ExpressionWrapper, LambdaExpression>();
            var form = new ExpressionCanonicalForm(validator, parametersToExtract);
            var key = new ExpressionWrapper(form.CanonicalForm, false);
            LambdaExpression lambda;
            if (!Cache.TryGetValue(key, out lambda))
            {
                Cache[key] = lambda = Expression.Lambda(form.CanonicalForm, form.ParameterAccessor);
            }
            return form.ConstructInvokation(lambda);
        }

        [ThreadStatic]
        private static Dictionary<ExpressionWrapper, LambdaExpression> Cache;
    }
}
