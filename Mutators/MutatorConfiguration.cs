using System;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public abstract class MutatorConfiguration
    {
        protected MutatorConfiguration(Type type)
        {
            Type = type;
        }

        public abstract MutatorConfiguration ToRoot(LambdaExpression path);
        public abstract MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer);
        public abstract MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver);
        public abstract MutatorConfiguration If(LambdaExpression condition);
        public abstract void GetArrays(ArraysExtractor arraysExtractor);

        public Type Type { get; private set; }

        public LambdaExpression[] Dependencies { get { return dependencies ?? (dependencies = GetDependencies()); } }

        protected abstract LambdaExpression[] GetDependencies();

        protected static LambdaExpression Prepare(LambdaExpression expression)
        {
            if (expression == null) return null;
//            if(expression.Body.NodeType == ExpressionType.Convert)
            //              expression = Expression.Lambda(((UnaryExpression)expression.Body).Operand, expression.Parameters);
            return (LambdaExpression)new IsNullOrEmptyExtender().Visit(expression.Simplify().RemoveLinqFirstAndSingle().ResolveInterfaceMembers());
        }

        protected static Expression PrepareForAssign(Expression path)
        {
            if (path.NodeType == ExpressionType.ArrayIndex)
            {
                var binaryExpression = (BinaryExpression)path;
                return Expression.ArrayAccess(binaryExpression.Left, binaryExpression.Right);
            }

            if (path.NodeType == ExpressionType.Convert)
                return ((UnaryExpression)path).Operand;
            return path;
        }

        protected static Expression Convert(Expression value, Type type)
        {
            // note ich: обязательно надо конвертить сначала к object-у, а только потом к type-у. Иначе получим очень странное исключение из потрохов Expression-ов, видимо баг
            return value.Type == type ? value : Expression.Convert(Expression.Convert(value, typeof(object)), type);
        }

        protected static LambdaExpression Resolve(Expression path, CompositionPerformer performer, LambdaExpression lambda)
        {
            if (lambda == null) return null;
            var body = performer.Perform(ExpressionExtensions.ResolveAbstractPath(Expression.Lambda(path, path.ExtractParameters()), lambda).Body).CanonizeParameters();
            return body == null ? null : Expression.Lambda(body, body.ExtractParameters());
        }

        private Expression lcp;

        private LambdaExpression[] dependencies;
    }
}