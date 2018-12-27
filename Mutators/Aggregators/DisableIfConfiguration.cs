using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Aggregators
{
    public class DisableIfConfiguration : AggregatorConfiguration
    {
        public DisableIfConfiguration(Type type, LambdaExpression condition)
            : base(type)
        {
            Condition = condition;
        }

        public override string ToString()
        {
            return "disabledIf" + (Condition == null ? "" : "(" + Condition + ")");
        }

        public static DisableIfConfiguration Create<TData>(Expression<Func<TData, bool?>> condition)
        {
            return new DisableIfConfiguration(typeof(TData), Prepare(condition));
        }

        public Expression GetCondition(List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (Condition == null) return null;
            return Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
        }

        internal override MutatorConfiguration ToRoot(LambdaExpression path)
        {
// ReSharper disable ConvertClosureToMethodGroup
            return new DisableIfConfiguration(path.Parameters.Single().Type, path.Merge(Condition));
// ReSharper restore ConvertClosureToMethodGroup
        }

        internal override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new DisableIfConfiguration(to, Resolve(path, performer, Condition));
        }

        internal override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new DisableIfConfiguration(Type, resolver.Resolve(Condition));
        }

        internal override MutatorConfiguration If(LambdaExpression condition)
        {
            return new DisableIfConfiguration(Type, Prepare(condition).AndAlso(Condition));
        }

        internal override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
        }

        public LambdaExpression Condition { get; private set; }

        protected internal override LambdaExpression[] GetDependencies()
        {
            return Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type));
        }
    }
}