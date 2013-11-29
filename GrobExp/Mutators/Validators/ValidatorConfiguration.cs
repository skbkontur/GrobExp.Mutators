using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators.Aggregators;

namespace GrobExp.Mutators.Validators
{
    public abstract class ValidatorConfiguration : AggregatorConfiguration
    {
        protected ValidatorConfiguration(Type type)
            : base(type)
        {
        }

        public abstract Expression Apply(List<KeyValuePair<Expression, Expression>> aliases);
    }
}