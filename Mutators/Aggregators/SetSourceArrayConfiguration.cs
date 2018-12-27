using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Aggregators
{
    public class SetSourceArrayConfiguration : AggregatorConfiguration
    {
        protected SetSourceArrayConfiguration(Type type, LambdaExpression sourceArray)
            : base(type)
        {
            SourceArray = sourceArray;
        }

        public LambdaExpression SourceArray { get; }
        
        internal static SetSourceArrayConfiguration Create(LambdaExpression sourceArray)
        {
            return new SetSourceArrayConfiguration(sourceArray.Parameters.Single().Type, Prepare(sourceArray));
        }

        internal override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            return new SetSourceArrayConfiguration(path.Parameters.Single().Type, path.Merge(SourceArray));
        }

        internal override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new SetSourceArrayConfiguration(to, Resolve(path, performer, SourceArray));
        }

        internal override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new SetSourceArrayConfiguration(Type, resolver.Resolve(SourceArray));
        }

        internal override MutatorConfiguration If(LambdaExpression condition)
        {
            throw new NotSupportedException();
        }

        internal override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(SourceArray);
        }

        protected internal override LambdaExpression[] GetDependencies()
        {
            return new LambdaExpression[0];
        }
    }
}