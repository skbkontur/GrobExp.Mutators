using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class SetArrayLengthConfiguration : AutoEvaluatorConfiguration
    {
        public SetArrayLengthConfiguration(Type type, LambdaExpression condition, LambdaExpression length)
            : base(type)
        {
            Condition = condition;
            Length = length;
        }

        internal override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
            arraysExtractor.GetArrays(Length);
        }

        public static SetArrayLengthConfiguration Create(Type type, LambdaExpression condition, LambdaExpression length)
        {
            return new SetArrayLengthConfiguration(type, Prepare(condition), Prepare(length));
        }

        public static SetArrayLengthConfiguration Create<TData>(Expression<Func<TData, bool?>> condition, Expression<Func<TData, int>> length)
        {
            return new SetArrayLengthConfiguration(typeof(TData), Prepare(condition), Prepare(length));
        }

        internal override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new SetArrayLengthConfiguration(path.Parameters.Single().Type, path.Merge(Condition), path.Merge(Length));
            // ReSharper restore ConvertClosureToMethodGroup
        }

        internal override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new SetArrayLengthConfiguration(to, Resolve(path, performer, Condition), Resolve(path, performer, Length));
        }

        internal override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new SetArrayLengthConfiguration(Type, resolver.Resolve(Condition), Length);
        }

        internal override MutatorConfiguration If(LambdaExpression condition)
        {
            return new SetArrayLengthConfiguration(Type, Prepare(condition).AndAlso(Condition), Length);
        }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            var temp = Expression.Variable(path.Type);
            var resize = Expression.Call(arrayResizeMethod, temp, Length.Body.ResolveAliases(aliases));
            var block = Expression.Block(new[] {temp}, Expression.Assign(temp, path), resize, Expression.Assign(PrepareForAssign(path), temp));
            if (Condition == null)
                return block;
            var condition = Condition.Body;
            condition = Expression.Equal(Expression.Convert(condition.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            return Expression.IfThen(condition, block);
        }

        public LambdaExpression Condition { get; private set; }
        public LambdaExpression Length { get; set; }

        protected internal override LambdaExpression[] GetDependencies()
        {
            return (Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type)))
                .Concat(Length == null ? new LambdaExpression[0] : Length.ExtractDependencies(Length.Parameters.Where(parameter => parameter.Type == Type)))
                .GroupBy(lambda => ExpressionCompiler.DebugViewGetter(lambda))
                .Select(grouping => grouping.First())
                .ToArray();
        }

        private static readonly MethodInfo arrayResizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 1))).Body).Method;
    }
}