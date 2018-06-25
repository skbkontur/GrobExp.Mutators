using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrobExp.Mutators.Aggregators;

namespace GrobExp.Mutators.Validators
{
    public enum MutatorsCreator
    {
        Sharp,
        VLang
    }

    public abstract class ValidatorConfiguration : AggregatorConfiguration
    {
        protected ValidatorConfiguration(Type type, MutatorsCreator creator, int priority)
            : base(type)
        {
            Creator = creator;
            Priority = priority;
        }

        public abstract Expression Apply(List<KeyValuePair<Expression, Expression>> aliases);
        public MutatorsCreator Creator { get; set; }
        public int Priority { get; private set; }
    }
}