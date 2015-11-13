using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;

namespace GrobExp.Mutators.Visitors
{
    public sealed class MethodExtractor : ExpressionVisitor
    {
        public Action<object[]> Method { get; private set; }
        public ParameterExpression[] Parameters { get; private set; }
        private readonly ParameterExpression accessor;

        public MethodExtractor(Expression expression)
        {
            Parameters = expression.ExtractParameters();
            accessor = Expression.Parameter(typeof(object[]));
            Method = (Action<object[]>)LambdaCompiler.Compile(Expression.Lambda(Visit(expression), accessor), CompilerOptions.All);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var index = Array.IndexOf(Parameters, node);
            if(0 <= index && index < Parameters.Length)
            {
                return Expression.Convert(Expression.ArrayAccess(accessor, Expression.Constant(index, typeof(int))), node.Type);
            }
            return base.VisitParameter(node);
        }
    }
}
