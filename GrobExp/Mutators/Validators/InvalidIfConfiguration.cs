using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Validators
{
    public class InvalidIfConfiguration : ValidatorConfiguration
    {
        protected InvalidIfConfiguration(Type type, int priority, LambdaExpression condition, LambdaExpression message, ValidationResultType validationResultType)
            : base(type, priority)
        {
            Condition = condition;
            Message = message;
            this.validationResultType = validationResultType;
        }

        public static InvalidIfConfiguration Create<TData>(int priority, Expression<Func<TData, bool?>> condition, Expression<Func<TData, MultiLanguageTextBase>> message, ValidationResultType validationResultType)
        {
            return new InvalidIfConfiguration(typeof(TData), priority, Prepare(condition), Prepare(message), validationResultType);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new InvalidIfConfiguration(path.Parameters.Single().Type, Priority, path.Merge(Condition), path.Merge(Message), validationResultType);
            // ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new InvalidIfConfiguration(to, Priority, Resolve(path, performer, Condition), Resolve(path, performer, Message), validationResultType);
        }

        public override MutatorConfiguration ResolveAliases(AliasesResolver resolver)
        {
            return new InvalidIfConfiguration(Type, Priority, (LambdaExpression)resolver.Visit(Condition), (LambdaExpression)resolver.Visit(Message), validationResultType);
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new InvalidIfConfiguration(Type, Priority, Prepare(condition).AndAlso(Condition), Message, validationResultType);
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
            arraysExtractor.GetArrays(Message);
        }

        public override Expression Apply(List<KeyValuePair<Expression, Expression>> aliases)
        {
            if(Condition == null) return null;
            var condition = Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            var message = Message == null ? Expression.Constant(null, typeof(MultiLanguageTextBase)) : Message.Body.ResolveAliases(aliases);
            var result = Expression.Variable(typeof(ValidationResult));
            var invalid = Expression.New(validationResultConstructor, Expression.Constant(validationResultType), message);
            var assign = Expression.IfThenElse(condition, Expression.Assign(result, invalid), Expression.Assign(result, Expression.Constant(ValidationResult.Ok)));
            return Expression.Block(new[] {result}, assign, result);
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

        protected override Expression GetLCP()
        {
            return (Condition == null ? new Expression[0] : Condition.Body.CutToChains(false, false)).Concat(Message == null ? new Expression[0] : Message.Body.CutToChains(false, false)).FindLCP();
        }

        private readonly ValidationResultType validationResultType;

        private static readonly ConstructorInfo validationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult>>)(() => new ValidationResult(ValidationResultType.Ok, null))).Body).Constructor;
    }
}