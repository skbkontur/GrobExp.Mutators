using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit.Utils;

using GrobExp.Compiler;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.MutatorsRecording;
using GrobExp.Mutators.MutatorsRecording.ValidationRecording;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Validators
{
    public class RequiredIfConfiguration : ValidatorConfiguration
    {
        protected RequiredIfConfiguration(Type type, MutatorsCreator creator, int priority, LambdaExpression condition, LambdaExpression path, LambdaExpression message, ValidationResultType validationResultType)
            : base(type, creator, priority)
        {
            Condition = condition;
            Path = (LambdaExpression)path.ReplaceEachWithCurrent();
            if (Path.Body.NodeType == ExpressionType.Constant)
                throw new ArgumentNullException(nameof(path));
            Message = message;
            this.validationResultType = validationResultType;
        }

        public LambdaExpression Condition { get; }
        public LambdaExpression Path { get; }
        public LambdaExpression Message { get; }

        public override string ToString()
        {
            return "requiredIf" + (Condition == null ? "" : "(" + Condition + ")");
        }

        public LambdaExpression GetFullCondition()
        {
            if (fullCondition != null)
                return fullCondition;
            Expression condition = Prepare(Expression.Lambda(Expression.Convert(CheckIfEmpty(Path.Body), typeof(bool?)), Path.Parameters)).Body;
            if (Condition != null)
            {
                var parameterFromPath = Path.Parameters.Single();
                var parameterFromCondition = Condition.Parameters.SingleOrDefault(parameter => parameter.Type == parameterFromPath.Type);
                if (parameterFromCondition != null)
                    condition = new ParameterReplacer(parameterFromPath, parameterFromCondition).Visit(condition);
                condition = Expression.AndAlso(Expression.Convert(Condition.Body, typeof(bool?)), Expression.Convert(condition, typeof(bool?)));
            }

            return fullCondition = Expression.Lambda(condition, condition.ExtractParameters());
        }

        public static RequiredIfConfiguration Create<TData, TValue>(MutatorsCreator creator, int priority, Expression<Func<TData, bool?>> condition, Expression<Func<TData, TValue>> path, Expression<Func<TData, MultiLanguageTextBase>> message, ValidationResultType validationResultType)
        {
            return new RequiredIfConfiguration(typeof(TData), creator, priority, Prepare(condition), Prepare(path), Prepare(message), validationResultType);
        }

        public static RequiredIfConfiguration Create<TData>(MutatorsCreator creator, int priority, LambdaExpression condition, LambdaExpression path, Expression<Func<TData, MultiLanguageTextBase>> message, ValidationResultType validationResultType)
        {
            return new RequiredIfConfiguration(typeof(TData), creator, priority, Prepare(condition), Prepare(path), Prepare(message), validationResultType);
        }

        internal override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new RequiredIfConfiguration(path.Parameters.Single().Type, Creator, Priority, path.Merge(Condition), path.Merge(Path), path.Merge(Message), validationResultType);
            // ReSharper restore ConvertClosureToMethodGroup
        }

        internal override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            var resolvedPath = Resolve(path, performer, Path);
            if (resolvedPath.Body.NodeType == ExpressionType.Constant)
                return null;
            return new RequiredIfConfiguration(to, Creator, Priority, Resolve(path, performer, Condition), resolvedPath, Resolve(path, performer, Message), validationResultType);
        }

        internal override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new RequiredIfConfiguration(Type, Creator, Priority, resolver.Resolve(Condition), resolver.Resolve(Path), resolver.Resolve(Message), validationResultType);
        }

        internal override MutatorConfiguration If(LambdaExpression condition)
        {
            return new RequiredIfConfiguration(Type, Creator, Priority, Prepare(condition).AndAlso(Condition), Path, Message, validationResultType);
        }

        internal override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
            arraysExtractor.GetArrays(Path);
            arraysExtractor.GetArrays(Message);
        }

        internal override Expression Apply(Type converterType, List<KeyValuePair<Expression, Expression>> aliases)
        {
            var condition = GetFullCondition().Body.ResolveAliases(aliases);
            var message = Message == null ? Expression.Constant(null, typeof(MultiLanguageTextBase)) : Message.Body.ResolveAliases(aliases);
            var result = Expression.Variable(typeof(ValidationResult));
            var invalid = Expression.New(validationResultConstructor, Expression.Constant(validationResultType), message);
            var assign = Expression.IfThenElse(Expression.Convert(condition, typeof(bool)), Expression.Assign(result, invalid), Expression.Assign(result, Expression.Constant(ValidationResult.Ok)));
            var toLog = new ValidationLogInfo("required", condition.ToString());
            if (MutatorsValidationRecorder.IsRecording())
                MutatorsValidationRecorder.RecordCompilingValidation(converterType, toLog);
            return Expression.Block(new[] {result}, assign, Expression.Call(RecordingMethods.RecordExecutingValidationMethodInfo, Expression.Constant(converterType, typeof(Type)), Expression.Constant(toLog), Expression.Call(result, typeof(object).GetMethod("ToString"))), result);
        }

        protected internal override LambdaExpression[] GetDependencies()
        {
            var condition = GetFullCondition();
            return (condition.ExtractDependencies(condition.Parameters.Where(parameter => parameter.Type == Type)))
                .Concat(Message == null ? new LambdaExpression[0] : Message.ExtractDependencies())
                .GroupBy(lambda => ExpressionCompiler.DebugViewGetter(lambda))
                .Select(grouping => grouping.First())
                .ToArray();
        }

        private static Expression CheckIfEmpty(Expression exp)
        {
            if (exp.Type == typeof(string))
                return Expression.Call(stringIsNullOrEmptyMethod, exp);
            var itemType = exp.Type.TryGetItemType();
            if (itemType != null)
            {
                if (itemType == typeof(string))
                    return Expression.Call(MutatorsHelperFunctions.StringArrayIsNullOrEmptyMethod, exp);
                return Expression.Not(Expression.Call(anyMethod.MakeGenericMethod(itemType), exp));
            }

            if (exp.Type.IsValueType && !exp.Type.IsNullable())
                return Expression.Constant(false);
            return Expression.Equal(exp, Expression.Constant(null, exp.Type));
        }

        private LambdaExpression fullCondition;

        private readonly ValidationResultType validationResultType;

        private static readonly ConstructorInfo validationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult>>)(() => new ValidationResult(ValidationResultType.Ok, null))).Body).Constructor;
        private static readonly MethodInfo anyMethod = HackHelpers.GetMethodDefinition<int[]>(x => x.Any()).GetGenericMethodDefinition();
        private static readonly MethodInfo stringIsNullOrEmptyMethod = HackHelpers.GetMethodDefinition<string>(s => string.IsNullOrEmpty(s));
    }
}