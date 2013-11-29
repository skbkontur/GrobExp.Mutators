using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class DisableIfConfiguration : AutoEvaluatorConfiguration
    {
        public DisableIfConfiguration(Type type, LambdaExpression condition)
            : base(type)
        {
            Condition = condition;
        }

        public static DisableIfConfiguration Create<TData>(Expression<Func<TData, bool?>> condition)
        {
            return new DisableIfConfiguration(typeof(TData), Prepare(condition));
        }

        public Expression GetCondition(List<KeyValuePair<Expression, Expression>> aliases)
        {
            if(Condition == null) return null;
            return Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
// ReSharper disable ConvertClosureToMethodGroup
            return new DisableIfConfiguration(path.Parameters.Single().Type, path.Merge(Condition));
// ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new DisableIfConfiguration(to, Resolve(path, performer, Condition));
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new DisableIfConfiguration(Type, Prepare(condition).AndAlso(Condition));
        }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            return null;
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
        }

        public LambdaExpression Condition { get; private set; }

        protected override LambdaExpression[] GetDependencies()
        {
            return Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type));
        }

        protected override Expression GetLCP()
        {
            return Condition == null ? null : Condition.Body.CutToChains(false, false).FindLCP();
        }
    }
}