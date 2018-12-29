using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.AutoEvaluators
{
    public abstract class AutoEvaluatorConfiguration : MutatorConfiguration
    {
        protected AutoEvaluatorConfiguration(Type type)
            : base(type)
        {
        }

        internal abstract Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases);
    }
}