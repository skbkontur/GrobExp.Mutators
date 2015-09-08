using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.AssignRecording;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Validators
{
    public class StaticValidatorConfiguration : ValidatorConfiguration
    {
        protected StaticValidatorConfiguration(Type type, string name, int priority, LambdaExpression condition, LambdaExpression path, LambdaExpression validator)
            : base(type, priority)
        {
            Name = name;
            Condition = condition;
            Path = (LambdaExpression)new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(path);
            validator = Prepare(validator);
            this.validator = validator;
            validatorFromRoot = path.Merge(validator);
        }

        public override string ToString()
        {
            return Name;
        }

        public static StaticValidatorConfiguration Create<TData, TValue>(string name, int priority, Expression<Func<TData, bool?>> condition, Expression<Func<TData, TValue>> path, Expression<Func<TValue, ValidationResult>> validator)
        {
            return new StaticValidatorConfiguration(typeof(TData), name, priority, Prepare(condition), Prepare(path), validator);
        }

        public static StaticValidatorConfiguration Create<TData>(string name, int priority, LambdaExpression condition, LambdaExpression path, LambdaExpression validator)
        {
            return new StaticValidatorConfiguration(typeof(TData), name, priority, Prepare(condition), Prepare(path), validator);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            return new StaticValidatorConfiguration(path.Parameters.Single().Type, Name, Priority, path.Merge(Condition), path.Merge(Path), validator);
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new StaticValidatorConfiguration(to, Name, Priority, Resolve(path, performer, Condition), Resolve(path, performer, Path), validator);
        }

        public override MutatorConfiguration ResolveAliases(AliasesResolver resolver)
        {
            return new StaticValidatorConfiguration(Type, Name, Priority, (LambdaExpression)resolver.Visit(Condition), (LambdaExpression)resolver.Visit(Path), validator);
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new StaticValidatorConfiguration(Type, Name, Priority, Prepare(condition).AndAlso(Condition), Path, validator);
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
            Expression condition = Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            var toLog = new ValidationLogInfo(Name, condition.ToString());
            var result = Expression.Variable(typeof(ValidationResult));
            condition = Expression.Condition(condition, validatorFromRoot.Body.ResolveAliases(aliases), Expression.Constant(ValidationResult.Ok));
            var assign = Expression.Assign(result, condition);
            if(MutatorsValidationRecorder.IsRecording())
                MutatorsValidationRecorder.RecordCompilingValidation(toLog);
            return Expression.Block(new [] { result }, assign, Expression.Call(typeof(MutatorsValidationRecorder).GetMethod("RecordExecutingValidation"), Expression.Constant(toLog), Expression.Call(result, typeof(object).GetMethod("ToString"))), result);
        }

        public string Name { get; set; }
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

        private readonly LambdaExpression validatorFromRoot;
        private readonly LambdaExpression validator;
    }
}