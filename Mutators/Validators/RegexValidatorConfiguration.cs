using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.MutatorsRecording;
using GrobExp.Mutators.MutatorsRecording.ValidationRecording;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Validators
{
    public class RegexValidatorConfiguration : ValidatorConfiguration
    {
        protected RegexValidatorConfiguration(Type type, MutatorsCreator creator, int priority, LambdaExpression path, LambdaExpression condition, LambdaExpression message, string pattern, Regex regex, ValidationResultType validationResultType)
            : base(type, creator, priority)
        {
            this.regex = regex;
            this.validationResultType = validationResultType;
            Path = (LambdaExpression)path.ReplaceEachWithCurrent();
            Condition = condition;
            Message = message;
            Pattern = pattern;
        }

        public static RegexValidatorConfiguration Create<TData>(MutatorsCreator creator, int priority, Expression<Func<TData, string>> path, Expression<Func<TData, bool?>> condition, Expression<Func<TData, MultiLanguageTextBase>> message, string pattern, ValidationResultType validationResultType)
        {
            return new RegexValidatorConfiguration(typeof(TData), creator, priority, Prepare(path), Prepare(condition), Prepare(message), pattern ?? "", new Regex(PreparePattern(pattern ?? ""), RegexOptions.Compiled), validationResultType);
        }

        public static RegexValidatorConfiguration Create<TData>(MutatorsCreator creator, int priority, LambdaExpression path, LambdaExpression condition, Expression<Func<TData, MultiLanguageTextBase>> message, string pattern, ValidationResultType validationResultType)
        {
            return new RegexValidatorConfiguration(typeof(TData), creator, priority, Prepare(path), Prepare(condition), Prepare(message), pattern ?? "", new Regex(PreparePattern(pattern ?? ""), RegexOptions.Compiled), validationResultType);
        }

        internal override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new RegexValidatorConfiguration(path.Parameters.Single().Type, Creator, Priority, path.Merge(Path), path.Merge(Condition), path.Merge(Message), Pattern, regex, validationResultType);
            // ReSharper restore ConvertClosureToMethodGroup
        }

        internal override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new RegexValidatorConfiguration(to, Creator, Priority, Resolve(path, performer, Path), Resolve(path, performer, Condition), Resolve(path, performer, Message), Pattern, regex, validationResultType);
        }

        internal override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new RegexValidatorConfiguration(Type, Creator, Priority, Path, resolver.Resolve(Condition), resolver.Resolve(Message), Pattern, regex, validationResultType);
        }

        internal override MutatorConfiguration If(LambdaExpression condition)
        {
            return new RegexValidatorConfiguration(Type, Creator, Priority, Path, Prepare(condition).AndAlso(Condition), Message, Pattern, regex, validationResultType);
        }

        internal override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
            arraysExtractor.GetArrays(Path);
            arraysExtractor.GetArrays(Message);
        }

        public LambdaExpression GetFullCondition()
        {
            if (fullCondition != null)
                return fullCondition;
            Expression valueIsNotNull = Prepare(Expression.Lambda(Expression.NotEqual(Path.Body, Expression.Constant(null, Path.Body.Type)), Path.Parameters)).Body;
            Expression condition = Expression.Convert(Expression.AndAlso(valueIsNotNull, Expression.Not(Expression.Call(Expression.Constant(regex), regexIsMatchMethod, Path.Body))), typeof(bool?));
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

        public override Expression Apply(Type converterType, List<KeyValuePair<Expression, Expression>> aliases)
        {
            var condition = GetFullCondition().Body.ResolveAliases(aliases);
            var message = Message == null ? Expression.Constant(null, typeof(MultiLanguageTextBase)) : Message.Body.ResolveAliases(aliases);
            var result = Expression.Variable(typeof(ValidationResult));
            var invalid = Expression.New(validationResultConstructor, Expression.Constant(validationResultType), message);
            var assign = Expression.IfThenElse(Expression.Convert(condition, typeof(bool)), Expression.Assign(result, invalid), Expression.Assign(result, Expression.Constant(ValidationResult.Ok)));
            var toLog = new ValidationLogInfo("Regex = " + regex, condition.ToString());

            if (MutatorsValidationRecorder.IsRecording())
                MutatorsValidationRecorder.RecordCompilingValidation(converterType, toLog);
            return Expression.Block(new[] {result}, Expression.Call(RecordingMethods.RecordExecutingValidationMethodInfo, Expression.Constant(converterType, typeof(Type)), Expression.Constant(toLog), Expression.Call(result, typeof(object).GetMethod("ToString"))), assign, result);
        }

        public LambdaExpression Path { get; private set; }
        public LambdaExpression Condition { get; set; }
        public LambdaExpression Message { get; private set; }
        public string Pattern { get; private set; }

        protected override LambdaExpression[] GetDependencies()
        {
            var condition = GetFullCondition();
            return (condition.ExtractDependencies(condition.Parameters.Where(parameter => parameter.Type == Type)))
                .Concat(Message == null ? new LambdaExpression[0] : Message.ExtractDependencies())
                .GroupBy(lambda => ExpressionCompiler.DebugViewGetter(lambda))
                .Select(grouping => grouping.First())
                .ToArray();
        }

        private static string PreparePattern(string pattern)
        {
            if (pattern[0] != '^')
                pattern = "^" + pattern;
            if (pattern[pattern.Length - 1] != '$')
                pattern = pattern + "$";
            return pattern;
        }

        private LambdaExpression fullCondition;
        private readonly Regex regex;
        private readonly ValidationResultType validationResultType;

        private static readonly ConstructorInfo validationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult>>)(() => new ValidationResult(ValidationResultType.Ok, null))).Body).Constructor;
        private static readonly MethodInfo regexIsMatchMethod = ((MethodCallExpression)((Expression<Func<Regex, bool>>)(regex => regex.IsMatch(""))).Body).Method;
    }
}