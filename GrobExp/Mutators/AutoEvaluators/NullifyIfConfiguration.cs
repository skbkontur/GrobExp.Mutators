using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class NullifyIfConfiguration : AutoEvaluatorConfiguration
    {
        public NullifyIfConfiguration(Type type, LambdaExpression condition)
            : base(type)
        {
            Condition = condition;
        }

        public override string ToString()
        {
            return "nullifiedIf" + (Condition == null ? "" : "(" + Condition + ")");
        }

        public static NullifyIfConfiguration Create<TData>(Expression<Func<TData, bool?>> condition)
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

        public override MutatorConfiguration ResolveAliases(AliasesResolver resolver)
        {
            return new NullifyIfConfiguration(Type, (LambdaExpression)resolver.Visit(Condition));
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new NullifyIfConfiguration(Type, Prepare(condition).AndAlso(Condition));
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
        }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if(Condition == null) return null;
            var condition = Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            path = PrepareForAssign(path);
            return Expression.IfThen(condition, Expression.Assign(path, Expression.Constant(path.Type.GetDefaultValue(), path.Type)));
        }

        public LambdaExpression Condition { get; private set; }

        protected override LambdaExpression[] GetDependencies()
        {
            return Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type));
        }

        protected override Expression GetLCP()
        {
            return Condition == null ? null : Condition.Body.CutToChains(false, false).FindLCP();
        }
    }
}