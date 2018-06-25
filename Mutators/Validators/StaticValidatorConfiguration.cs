using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.MutatorsRecording.ValidationRecording;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Validators
{
    public class StaticValidatorConfiguration : ValidatorConfiguration
    {
        protected StaticValidatorConfiguration(Type type, MutatorsCreator creator, string name, int priority,
                                               LambdaExpression condition, LambdaExpression pathToNode, LambdaExpression pathToValue, LambdaExpression validator)
            : base(type, creator, priority)
        {
            Name = name;
            Condition = condition;
            var replacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            PathToNode = (LambdaExpression)replacer.Visit(pathToNode);
            PathToValue = (LambdaExpression)replacer.Visit(pathToValue);
            validator = Prepare(validator);
            this.validator = validator;
            validatorFromRoot = pathToNode.Merge(validator);
        }

        public override string ToString()
        {
            return Name;
        }

        public static StaticValidatorConfiguration Create<TData, TChild, TValue>(MutatorsCreator creator, string name, int priority,
                                                                                 Expression<Func<TData, bool?>> condition, Expression<Func<TData, TChild>> pathToNode,
                                                                                 Expression<Func<TData, TValue>> pathToValue, Expression<Func<TChild, ValidationResult>> validator)
        {
            return new StaticValidatorConfiguration(typeof(TData), creator, name, priority,
                                                    Prepare(condition), Prepare(pathToNode), Prepare(pathToValue), validator);
        }

        public static StaticValidatorConfiguration Create<TData>(MutatorsCreator creator, string name, int priority,
                                                                 LambdaExpression condition, LambdaExpression pathToNode, LambdaExpression pathToValue, LambdaExpression validator)
        {
            return new StaticValidatorConfiguration(typeof(TData), creator, name, priority,
                                                    Prepare(condition), Prepare(pathToNode), Prepare(pathToValue), validator);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            return new StaticValidatorConfiguration(path.Parameters.Single().Type, Creator, Name, Priority,
                                                    path.Merge(Condition), path.Merge(PathToNode), path.Merge(PathToValue), validator);
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new StaticValidatorConfiguration(to, Creator, Name, Priority,
                                                    Resolve(path, performer, Condition), Resolve(path, performer, PathToNode),
                                                    Resolve(path, performer, PathToValue), validator);
        }

        public override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new StaticValidatorConfiguration(Type, Creator, Name, Priority, resolver.Resolve(Condition), resolver.Resolve(PathToNode), resolver.Resolve(PathToValue), validator);
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new StaticValidatorConfiguration(Type, Creator, Name, Priority,
                                                    Prepare(condition).AndAlso(Condition), PathToNode, PathToValue, validator);
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
            arraysExtractor.GetArrays(validatorFromRoot);
        }

        public override Expression Apply(List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (Condition == null)
                return validatorFromRoot.Body.ResolveAliases(aliases);
            Expression condition = Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            var toLog = new ValidationLogInfo(Name, condition.ToString());
            var result = Expression.Variable(typeof(ValidationResult));
            condition = Expression.Condition(condition, validatorFromRoot.Body.ResolveAliases(aliases), Expression.Constant(ValidationResult.Ok));
            var assign = Expression.Assign(result, condition);
            if (MutatorsValidationRecorder.IsRecording())
                MutatorsValidationRecorder.RecordCompilingValidation(toLog);
            return Expression.Block(new[] {result}, assign, Expression.Call(typeof(MutatorsValidationRecorder).GetMethod("RecordExecutingValidation"), Expression.Constant(toLog), Expression.Call(result, typeof(object).GetMethod("ToString"))), result);
        }

        public string Name { get; set; }
        public LambdaExpression Condition { get; private set; }
        public LambdaExpression PathToValue { get; private set; }
        public LambdaExpression PathToNode { get; private set; }

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