using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.MutatorsRecording.AssignRecording;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class EqualsToConfiguration : AutoEvaluatorConfiguration
    {
        protected EqualsToConfiguration(Type converterType, Type type, LambdaExpression value, StaticValidatorConfiguration validator)
            : base(type)
        {
            ConverterType = converterType;
            Value = value;
            Validator = validator;
        }

        public override string ToString()
        {
            return "equalsTo" + ("(" + Value + ")");
        }

        public static EqualsToConfiguration Create<TData>(Type converterType, LambdaExpression value, StaticValidatorConfiguration validator = null)
        {
            return new EqualsToConfiguration(converterType, typeof(TData), Prepare(value), validator);
        }

        public static EqualsToConfiguration Create(Type converterType, Type type, LambdaExpression value, StaticValidatorConfiguration validator)
        {
            return new EqualsToConfiguration(converterType, type, Prepare(value), validator);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new EqualsToConfiguration(ConverterType, path.Parameters.Single().Type, path.Merge(Value), Validator);
            // ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            if (Validator != null)
                throw new NotSupportedException();
            return new EqualsToConfiguration(ConverterType, to, Resolve(path, performer, Value), Validator);
        }

        public override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new EqualsToConfiguration(ConverterType, Type, resolver.Resolve(Value), Validator == null ? null : (StaticValidatorConfiguration)Validator.ResolveAliases(resolver));
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new EqualsToIfConfiguration(ConverterType, Type, Prepare(condition), Value, Validator == null ? null : (StaticValidatorConfiguration)Validator.If(condition));
        }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (Value == null) return null;
            var infoToLog = new AssignLogInfo(path, Value.Body);
            path = PrepareForAssign(path);
            var value = Convert(Value.Body.ResolveAliases(aliases), path.Type);
            return path.Assign(ConverterType, value, infoToLog);
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Value);
        }

        public Type ConverterType { get; }
        public LambdaExpression Value { get; }

        public StaticValidatorConfiguration Validator { get; }

        protected override LambdaExpression[] GetDependencies()
        {
            return Value == null ? new LambdaExpression[0] : Value.ExtractDependencies(Value.Parameters.Where(parameter => parameter.Type == Type));
        }
    }
}