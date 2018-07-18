using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;

namespace GrobExp.Mutators.Visitors
{
    /// <summary>
    ///     Inlines given expressions (paths) into an expression
    ///     <code>
    /// paths = [x => x.a, y => y.b]
    /// Merge(a, b => a + b) -> 
    /// x, y => x.a + y.b </code>
    /// </summary>
    public class ExpressionMerger : ExpressionVisitor
    {
        public ExpressionMerger(params LambdaExpression[] paths)
        {
            // todo ich: избавиться бы от HashSet-а..
            if (paths == null)
                throw new ArgumentNullException("paths");
            this.paths = paths.Select(path => path.Body).ToArray();
            parameters = new HashSet<ParameterExpression>(paths.SelectMany(lambda => lambda.Parameters)).ToArray();
        }

        public Expression<TDelegate> Merge<TDelegate>(LambdaExpression expression)
        {
            return LambdaExpressionCreator.Create<TDelegate>(MergeInternal(expression), parameters);
        }

        public LambdaExpression Merge(LambdaExpression expression)
        {
            return LambdaExpressionCreator.Create(MergeInternal(expression), parameters);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var index = expressionParameters.IndexOf(node);
            return index >= 0 ? paths[index] : base.VisitParameter(node);
        }

        private Expression MergeInternal(LambdaExpression expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");
            if (expression.Parameters.Count == 0)
                return expression.Body;
            if (expression.Parameters.Count != paths.Length)
                throw new ArgumentException("Expected lambda with exactly " + paths.Length + " parameters", "expression");
            for (var i = 0; i < paths.Length; ++i)
            {
                if (paths[i].Type != expression.Parameters[i].Type)
                    throw new InvalidOperationException("Type of 'paths[" + i + "]' is not equal to corresponding parameter type of 'expression'");
            }

            expressionParameters = expression.Parameters;
            return Visit(expression.Body);
        }

        private readonly Expression[] paths;
        private readonly ParameterExpression[] parameters;
        private ReadOnlyCollection<ParameterExpression> expressionParameters;
    }
}