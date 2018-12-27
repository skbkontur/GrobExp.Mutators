using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.MutatorsRecording.AssignRecording;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class EqualsToIfConfiguration : EqualsToConfiguration
    {
        public EqualsToIfConfiguration(Type converterType, Type type, LambdaExpression condition, LambdaExpression value, StaticValidatorConfiguration validator)
            : base(converterType, type, value, validator)
        {
            Condition = condition;
        }

        internal override void GetArrays(ArraysExtractor arraysExtractor)
        {
            base.GetArrays(arraysExtractor);
            arraysExtractor.GetArrays(Condition);
        }

        public override string ToString()
        {
            var value = "equalsTo (" + Value + ")";
            return Condition == null ? value : "if (" + Condition + ") " + value;
        }

        public static EqualsToIfConfiguration Create(Type converterType, Type type, LambdaExpression condition, LambdaExpression value, StaticValidatorConfiguration validator)
        {
            return new EqualsToIfConfiguration(converterType, type, Prepare(condition), Prepare(value), validator);
        }

        internal override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new EqualsToIfConfiguration(ConverterType, path.Parameters.Single().Type, path.Merge(Condition), path.Merge(Value), Validator);
            // ReSharper restore ConvertClosureToMethodGroup
        }

        internal override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            if (Validator != null)
                throw new NotSupportedException();
            return new EqualsToIfConfiguration(ConverterType, to, Resolve(path, performer, Condition), Resolve(path, performer, Value), Validator);
        }

        internal override MutatorConfiguration If(LambdaExpression condition)
        {
            return new EqualsToIfConfiguration(ConverterType, Type, Prepare(condition).AndAlso(Condition), Value, Validator == null ? null : (StaticValidatorConfiguration)Validator.If(condition));
        }

        internal override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new EqualsToIfConfiguration(ConverterType, Type, resolver.Resolve(Condition), resolver.Resolve(Value), Validator == null ? null : (StaticValidatorConfiguration)Validator.ResolveAliases(resolver));
        }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (Value == null) return null;
            var infoToLog = new AssignLogInfo(path, Value.Body);
            path = PrepareForAssign(path);
            var value = Convert(Value.Body.ResolveAliases(aliases), path.Type);
            var assignment = path.Assign(ConverterType, value, infoToLog);
            if (Condition == null)
                return assignment;
            var condition = Condition.Body;
            condition = Expression.Equal(Expression.Convert(condition.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            return Expression.IfThen(condition, assignment);
        }

        public LambdaExpression Condition { get; }

        protected override LambdaExpression[] GetDependencies()
        {
            return (Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type)))
                .Concat(Value == null ? new LambdaExpression[0] : Value.ExtractDependencies(Value.Parameters.Where(parameter => parameter.Type == Type)))
                .GroupBy(lambda => ExpressionCompiler.DebugViewGetter(lambda))
                .Select(grouping => grouping.First())
                .ToArray();
        }

//        protected override Expression[] GetChains()
//        {
//            return (Condition == null ? new Expression[0] : Condition.CutToChains(false))
//                .Concat(Value == null ? new Expression[0] : Value.CutToChains(false))
//                .GroupBy(expression => ExpressionCompiler.DebugViewGetter(expression))
//                .Select(grouping => grouping.First())
//                .ToArray();
//        }
    }
}