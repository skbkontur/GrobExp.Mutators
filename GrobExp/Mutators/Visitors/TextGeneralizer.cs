using System;
using System.Linq.Expressions;

using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators.Visitors
{
    public class TextGeneralizer : ExpressionVisitor
    {
        public TextGeneralizer(Type type)
        {
            this.type = type;
            parameter = Expression.Parameter(typeof(MultiLanguageTextBase));
            convertedParameter = Expression.Convert(parameter, type);
        }

        public Expression<Func<MultiLanguageTextBase, string>> Generalize(Expression<Func<string>> lambda)
        {
            var body = Visit(lambda.Body);
            return Expression.Lambda<Func<MultiLanguageTextBase, string>>(body, parameter);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression != null && node.Expression.Type == type)
                return Expression.MakeMemberAccess(convertedParameter, node.Member);
            return base.VisitMember(node);
        }

        private readonly Expression convertedParameter;
        private readonly ParameterExpression parameter;
        private readonly Type type;
    }
}