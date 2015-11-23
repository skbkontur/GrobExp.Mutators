using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Compiler;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class ExpressionCanonicalForm
    {
        public Expression Source { get; private set; }
        public Expression CanonicalForm { get; private set; }
        public ParameterExpression ParameterAccessor { get; set; }
        public Expression[] ExtractedExpressions { get; set; }

        public ExpressionCanonicalForm(Expression source)
        {
            Source = source;
            ExtractedExpressions = new ChainsExtractor().Extract(Source)
                .Concat(new ConstantsExtractor().Extract(Source, false))
                .ToArray();
            CanonicalForm = new ExpressionCanonizer().Canonize(Source, ExtractedExpressions);
        }

        public LambdaExpression GetLambda()
        {
            var fieldNames = ExpressionTypeBuilder.GenerateFieldNames(ExtractedExpressions);
            var builtType = ExpressionTypeBuilder.BuildType(ExtractedExpressions, fieldNames);
            var fieldInfos = ExpressionTypeBuilder.GetFieldInfos(builtType, fieldNames);
            ParameterAccessor = Expression.Parameter(builtType);
            var canonizedBody = new ExtractedExpressionsReplacer().Replace(Source, ExtractedExpressions, ParameterAccessor, fieldInfos);
            return Expression.Lambda(canonizedBody, ParameterAccessor);
        }

        public Expression ConstructInvokation(Delegate lambda)
        {
            var fieldNames = ExpressionTypeBuilder.GenerateFieldNames(ExtractedExpressions);
            var type = lambda.GetType().GetGenericArguments()[0];
            var closure = Expression.Parameter(type);
            var blockBody = new List<Expression>();
            blockBody.Add(Expression.Assign(closure, Expression.New(type)));
            for(var i = 0; i < ExtractedExpressions.Length; ++i)
            {
                var field = type.GetField(fieldNames[i]);
                blockBody.Add(Expression.Assign(Expression.Field(closure, field), ExtractedExpressions[i]));
            }
            blockBody.Add(Expression.Invoke(Expression.Constant(lambda), closure));
            return Expression.Block(new[] { closure }, blockBody);
        }
    }
}
