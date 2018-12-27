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

        public MutatorsCreator Creator { get; }

        internal abstract Expression Apply(Type converterType, List<KeyValuePair<Expression, Expression>> aliases);

        internal int Priority { get; }
    }
}