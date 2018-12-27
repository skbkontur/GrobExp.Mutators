using System;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public abstract class MutatorConfiguration
    {
        protected internal MutatorConfiguration(Type type)
        {
            Type = type;
        }

        public Type Type { get; }

        public LambdaExpression[] Dependencies => dependencies ?? (dependencies = GetDependencies());

        internal abstract MutatorConfiguration ToRoot(LambdaExpression path);
        internal abstract MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer);
        internal abstract MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver);
        internal abstract MutatorConfiguration If(LambdaExpression condition);
        internal abstract void GetArrays(ArraysExtractor arraysExtractor);

        protected internal abstract LambdaExpression[] GetDependencies();

        protected internal static LambdaExpression Prepare(LambdaExpression expression)
        {
            if (expression == null) return null;
//            if(expression.Body.NodeType == ExpressionType.Convert)
//              expression = Expression.Lambda(((UnaryExpression)expression.Body).Operand, expression.Parameters);
            return (LambdaExpression)new IsNullOrEmptyExtender().Visit(expression.Simplify().RemoveLinqFirstAndSingle().ResolveInterfaceMembers());
        }

        protected internal static Expression PrepareForAssign(Expression path)
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

        protected internal static Expression Convert(Expression value, Type type)
        {
            // note ich: обязательно надо конвертить сначала к object-у, а только потом к type-у. Иначе получим очень странное исключение из потрохов Expression-ов, видимо баг
            return value.Type == type ? value : Expression.Convert(Expression.Convert(value, typeof(object)), type);
        }

        protected internal static LambdaExpression Resolve(Expression path, CompositionPerformer performer, LambdaExpression lambda)
        {
            if (lambda == null) return null;
            var body = performer.Perform(ExpressionExtensions.ResolveAbstractPath(Expression.Lambda(path, path.ExtractParameters()), lambda).Body).CanonizeParameters();
            return body == null ? null : Expression.Lambda(body, body.ExtractParameters());
        }

        private LambdaExpression[] dependencies;
    }
}