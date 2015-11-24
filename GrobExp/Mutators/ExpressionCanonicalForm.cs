using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
                .Concat(new ConstantsExtractor().Extract(Source, true))
                .ToArray();
            CanonicalForm = new ExpressionCanonizer().Canonize(Source, ExtractedExpressions);
        }

        public LambdaExpression GetLambda()
        {
            var fieldNames = ExpressionTypeBuilder.GenerateFieldNames(ExtractedExpressions);
            FieldInfo[] fieldInfos;
            var builtType = ExpressionTypeBuilder.BuildType(ExtractedExpressions, fieldNames, out fieldInfos);
            ParameterAccessor = Expression.Parameter(builtType);
            var canonizedBody = new ExtractedExpressionsReplacer().Replace(Source, ExtractedExpressions, ParameterAccessor, fieldInfos);
            return Expression.Lambda(canonizedBody, ParameterAccessor);
        }

        public Expression ConstructInvokation(Delegate lambda)
        {
            var fieldNames = ExpressionTypeBuilder.GenerateFieldNames(ExtractedExpressions);
            var type = lambda.GetType().GetGenericArguments()[0];
            var closure = Expression.Parameter(type);

            var blockBody = new List<Expression> {Expression.Assign(closure, Expression.New(type))};
            blockBody.AddRange(fieldNames
                .Select(t => type.GetField(t))
                .Select((field, i) => Expression.Assign(Expression.Field(closure, field), ExtractedExpressions[i])));
            blockBody.Add(Expression.Invoke(Expression.Constant(lambda), closure));

            return Expression.Block(new[] { closure }, blockBody);
        }
    }
}
