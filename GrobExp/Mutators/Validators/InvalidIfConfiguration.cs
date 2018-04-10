using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.MutatorsRecording.ValidationRecording;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Validators
{
    public class InvalidIfConfiguration : ValidatorConfiguration
    {
        protected InvalidIfConfiguration(Type type, MutatorsCreator creator, int priority, LambdaExpression condition, LambdaExpression message, ValidationResultType validationResultType)
            : base(type, creator, priority)
        {
            Condition = condition;
            Message = message;
            this.validationResultType = validationResultType;
        }

        public override string ToString()
        {
            return "invalidIf" + (Condition == null ? "" : "(" + Condition + ")");
        }

        public static InvalidIfConfiguration Create<TData>(MutatorsCreator creator, int priority, Expression<Func<TData, bool?>> condition, Expression<Func<TData, MultiLanguageTextBase>> message, ValidationResultType validationResultType)
        {
            return new InvalidIfConfiguration(typeof(TData), creator, priority, Prepare(condition), Prepare(message), validationResultType);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new InvalidIfConfiguration(path.Parameters.Single().Type, Creator, Priority, path.Merge(Condition), path.Merge(Message), validationResultType);
            // ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new InvalidIfConfiguration(to, Creator, Priority, Resolve(path, performer, Condition), Resolve(path, performer, Message), validationResultType);
        }

        public override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new InvalidIfConfiguration(Type, Creator, Priority, resolver.Resolve(Condition), resolver.Resolve(Message), validationResultType);
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new InvalidIfConfiguration(Type, Creator, Priority, Prepare(condition).AndAlso(Condition), Message, validationResultType);
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
            arraysExtractor.GetArrays(Message);
        }

        public override Expression Apply(List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (Condition == null) return null;
            var condition = Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            var message = Message == null ? Expression.Constant(null, typeof(MultiLanguageTextBase)) : Message.Body.ResolveAliases(aliases);
            var result = Expression.Variable(typeof(ValidationResult));
            var invalid = Expression.New(validationResultConstructor, Expression.Constant(validationResultType), message);
            var assign = Expression.IfThenElse(condition, Expression.Assign(result, invalid), Expression.Assign(result, Expression.Constant(ValidationResult.Ok)));
            var toLog = new ValidationLogInfo("invalidIf", condition.ToString());
            if (MutatorsValidationRecorder.IsRecording())
                MutatorsValidationRecorder.RecordCompilingValidation(toLog);
            return Expression.Block(new[] {result}, assign, Expression.Call(typeof(MutatorsValidationRecorder).GetMethod("RecordExecutingValidation"), Expression.Constant(toLog), Expression.Call(result, typeof(object).GetMethod("ToString"))), result);
        }

        public LambdaExpression Condition { get; private set; }
        public LambdaExpression Message { get; private set; }

        protected override LambdaExpression[] GetDependencies()
        {
            return (Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type)))
                   .Concat(Message == null ? new LambdaExpression[0] : Message.ExtractDependencies())
                   .GroupBy(lambda => ExpressionCompiler.DebugViewGetter(lambda))
                   .Select(grouping => grouping.First())
                   .ToArray();
        }

        private readonly ValidationResultType validationResultType;

        private static readonly ConstructorInfo validationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult>>)(() => new ValidationResult(ValidationResultType.Ok, null))).Body).Constructor;
    }
}