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

        public override string ToString()
        {
            return "hiddenIf" + (Condition == null ? "" : "(" + Condition + ")");
        }

        public new static HideIfConfiguration Create<TData>(Expression<Func<TData, bool?>> condition)
        {
            return new HideIfConfiguration(typeof(TData), Prepare(condition));
        }

        internal override MutatorConfiguration ToRoot(LambdaExpression path)
        {
// ReSharper disable ConvertClosureToMethodGroup
            return new HideIfConfiguration(path.Parameters.Single().Type, path.Merge(Condition));
// ReSharper restore ConvertClosureToMethodGroup
        }

        internal override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new HideIfConfiguration(to, Resolve(path, performer, Condition));
        }

        internal override MutatorConfiguration If(LambdaExpression condition)
        {
            return new HideIfConfiguration(Type, Prepare(condition).AndAlso(Condition));
        }

        internal override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new HideIfConfiguration(Type, resolver.Resolve(Condition));
        }
    }
}