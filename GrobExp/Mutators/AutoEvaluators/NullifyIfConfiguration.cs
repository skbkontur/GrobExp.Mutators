using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class NullifyIfConfiguration : DisableIfConfiguration
    {
        protected NullifyIfConfiguration(Type type, LambdaExpression condition)
            : base(type, condition)
        {
        }

        public new static NullifyIfConfiguration Create<TData>(Expression<Func<TData, bool?>> condition)
        {
            return new NullifyIfConfiguration(typeof(TData), Prepare(condition));
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new NullifyIfConfiguration(path.Parameters.Single().Type, path.Merge(Condition));
            // ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new NullifyIfConfiguration(to, Resolve(path, performer, Condition));
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new NullifyIfConfiguration(Type, Prepare(condition).AndAlso(Condition));
        }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            var condition = GetCondition(aliases);
            if(condition == null) return null;
            return Expression.IfThen(condition, Expression.Assign(path, Expression.Constant(path.Type.GetDefaultValue(), path.Type)));
        }
    }
}