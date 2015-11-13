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
            var lambda = (Action<object[]>)canonicalFormsCache[key];
            if(lambda == null)
            {
                lock(lockObject)
                {
                    lambda = (Action<object[]>)canonicalFormsCache[key];
                    if(lambda == null)
                    {
                        canonicalFormsCache[key] = lambda = (Action<object[]>)LambdaCompiler.Compile(form.Lambda, CompilerOptions.All);
                    }
                }
            }
            return form.ConstructInvokation(lambda);
        }

        public static Expression GetCachedExpression(Expression validator)
        {
            object[] consts;
            var accessor = Expression.Parameter(typeof(object[]));
            var parameters = validator.ExtractParameters();
            var key = new ExpressionWrapper(new ExpressionConstantsExtractor(accessor).ExtractConstants(validator, out consts), false);
            var exp = expressionsCache[key];
            if(exp == null)
            {
                lock(lockObject2)
                {
                    exp = expressionsCache[key];
                    if(exp == null)
                    {
                        expressionsCache[key] = exp = LambdaCompiler.Compile(Expression.Lambda(key.Expression, parameters.Concat(new []{accessor})), CompilerOptions.All);
                    }
                }
            }
            return Expression.Invoke(Expression.Constant(exp), parameters.Cast<Expression>().Concat(new []{
                Expression.NewArrayInit(typeof(object), 
                consts.Select(c => Expression.Convert(Expression.Constant(c), typeof(object)))
                )}));
        }

        public static int FormsCount()
        {
            return canonicalFormsCache.Count;
        }

        public static int Count()
        {
            return expressionsCache.Count;
        }

        private static readonly Hashtable canonicalFormsCache = new Hashtable();
        private static readonly Hashtable expressionsCache = new Hashtable();
        private static readonly object lockObject = new object(), lockObject2 = new object();

        public static void Clear()
        {
            lock(lockObject)
            {
                canonicalFormsCache.Clear();
            }
            lock(lockObject2)
            {
                expressionsCache.Clear();
            }
        }
    }
}
