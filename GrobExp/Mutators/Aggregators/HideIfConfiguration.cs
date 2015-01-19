using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Aggregators
{
    public class HideIfConfiguration : DisableIfConfiguration
    {
        public HideIfConfiguration(Type type, LambdaExpression condition)
            : base(type, condition)
        {
        }

        public new static HideIfConfiguration Create<TData>(Expression<Func<TData, bool?>> condition)
        {
            return new HideIfConfiguration(typeof(TData), Prepare(condition));
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
// ReSharper disable ConvertClosureToMethodGroup
            return new HideIfConfiguration(path.Parameters.Single().Type, path.Merge(Condition));
// ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new HideIfConfiguration(to, Resolve(path, performer, Condition));
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new HideIfConfiguration(Type, Prepare(condition).AndAlso(Condition));
        }
    }
}