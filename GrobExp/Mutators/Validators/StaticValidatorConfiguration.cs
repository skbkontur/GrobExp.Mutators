using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Validators
{
    public class StaticValidatorConfiguration : ValidatorConfiguration
    {
        protected StaticValidatorConfiguration(Type type, int priority, LambdaExpression condition, LambdaExpression path, LambdaExpression validator)
            : base(type, priority)
        {
            Condition = condition;
            Path = (LambdaExpression)new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(path);
            validator = Prepare(validator);
            this.validator = validator;
            validatorFromRoot = path.Merge(validator);
        }

        public static StaticValidatorConfiguration Create<TData, TValue>(int priority, Expression<Func<TData, bool?>> condition, Expression<Func<TData, TValue>> path, Expression<Func<TValue, ValidationResult>> validator)
        {
            return new StaticValidatorConfiguration(typeof(TData), priority, Prepare(condition), Prepare(path), validator);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            return new StaticValidatorConfiguration(path.Parameters.Single().Type, Priority, path.Merge(Condition), path.Merge(Path), validator);
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new StaticValidatorConfiguration(to, Priority, Resolve(path, performer, Condition), Resolve(path, performer, Path), validator);
        }

        public override MutatorConfiguration ResolveAliases(AliasesResolver resolver)
        {
            return new StaticValidatorConfiguration(Type, Priority, (LambdaExpression)resolver.Visit(Condition), (LambdaExpression)resolver.Visit(Path), validator);
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new StaticValidatorConfiguration(Type, Priority, Prepare(condition).AndAlso(Condition), Path, validator);
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
            arraysExtractor.GetArrays(validatorFromRoot);
        }

        public override Expression Apply(List<KeyValuePair<Expression, Expression>> aliases)
        {
            if(Condition == null)
                return validatorFromRoot.Body.ResolveAliases(aliases);
            var condition = Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            return Expression.Condition(condition, validatorFromRoot.Body.ResolveAliases(aliases), Expression.Constant(null, typeof(ValidationResult)));
        }

        public LambdaExpression Condition { get; private set; }
        public LambdaExpression Path { get; private set; }

        protected override LambdaExpression[] GetDependencies()
        {
            return (Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type)))
                .Concat(validatorFromRoot == null ? new LambdaExpression[0] : validatorFromRoot.ExtractDependencies())
                .GroupBy(lambda => ExpressionCompiler.DebugViewGetter(lambda))
                .Select(grouping => grouping.First())
                .ToArray();
        }

        protected override Expression GetLCP()
        {
            return (Condition == null ? new LambdaExpression[0] : Condition.Body.CutToChains(false, false)).Concat(validatorFromRoot == null ? new Expression[0] : validatorFromRoot.Body.CutToChains(false, false)).FindLCP();
        }

        private readonly LambdaExpression validatorFromRoot;
        private readonly LambdaExpression validator;
    }
}