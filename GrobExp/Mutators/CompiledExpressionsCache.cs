using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Compiler;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class CompiledExpressionsCache
    {
        public static Expression GetCanonicalForm(Expression expression)
        {
            var form = new ExpressionCanonicalForm(expression);
            var key = ExpressionHashCalculator.CalcStrongHashCode(form.CanonicalForm);
            var lambda = (Delegate)canonicalFormsCache[key];
            if(lambda == null)
            {
                lock(lockObject)
                {
                    lambda = (Delegate)canonicalFormsCache[key];
                    if(lambda == null)
                    {
                        canonicalFormsCache[key] = lambda = LambdaCompiler.Compile(form.GetLambda(), CompilerOptions.All);
                    }
                }
            }
            return form.ConstructInvokation(lambda);
        }

        public static Expression GetCachedExpression(Expression validator)
        {
            var parameters = validator.ExtractParameters();
            var consts = new ConstantsExtractor().Extract(validator, false).Cast<Expression>().ToArray();
            var keyExpression = new ExpressionCanonizer().Canonize(validator, consts);
            var key = ExpressionHashCalculator.CalcStrongHashCode(keyExpression);
            var exp = (Delegate)expressionsCache[key];
            if(exp == null)
            {
                lock(lockObject2)
                {
                    exp = (Delegate)expressionsCache[key];
                    if(exp == null)
                    {
                        expressionsCache[key] = exp = LambdaCompiler.Compile(BuildLambda(validator, consts), CompilerOptions.All);
                    }
                }
            }
            return ConstructInvokation(exp, consts, parameters);
        }

        private static Expression ConstructInvokation(Delegate exp, Expression[] consts, ParameterExpression[] parameters)
        {
            var type = exp.GetType().GetGenericArguments()[0];
            var parameter = Expression.Parameter(type);
            var fieldNames = ExpressionTypeBuilder.GenerateFieldNames(consts);
            var body = new List<Expression> {Expression.Assign(parameter, Expression.New(type))};
            body.AddRange(fieldNames.Select(type.GetField).Select((field, i) => Expression.Assign(Expression.Field(parameter, field), consts[i])));
            body.Add(Expression.Invoke(Expression.Constant(exp), new[] {parameter}.Concat(parameters)));
            return Expression.Block(new [] {parameter}, body);
        }

        private static LambdaExpression BuildLambda(Expression validator, Expression[] consts)
        {
            var fieldNames = ExpressionTypeBuilder.GenerateFieldNames(consts);
            FieldInfo[] fieldInfos;
            var type = ExpressionTypeBuilder.GetType(consts, fieldNames, out fieldInfos);
            var parameterAccessor = Expression.Parameter(type);
            var body = new ExtractedExpressionsReplacer().Replace(validator, consts, parameterAccessor, fieldInfos);
            var otherParameters = validator.ExtractParameters();
            return Expression.Lambda(body, new []{parameterAccessor}.Concat(otherParameters));
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
