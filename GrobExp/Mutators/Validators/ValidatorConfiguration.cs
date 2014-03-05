using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators.Aggregators;

namespace GrobExp.Mutators.Validators
{
    public abstract class ValidatorConfiguration : AggregatorConfiguration
    {
        protected ValidatorConfiguration(Type type, int priority)
            : base(type)
        {
            Priority = priority;
        }

        public abstract Expression Apply(List<KeyValuePair<Expression, Expression>> aliases);
        public int Priority { get; private set; }
    }
}