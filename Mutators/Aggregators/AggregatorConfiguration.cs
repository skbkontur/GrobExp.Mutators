using System;

namespace GrobExp.Mutators.Aggregators
{
    public abstract class AggregatorConfiguration : MutatorConfiguration
    {
        protected AggregatorConfiguration(Type type)
            : base(type)
        {
        }
    }
}