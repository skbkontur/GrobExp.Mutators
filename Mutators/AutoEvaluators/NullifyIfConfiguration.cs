using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.MutatorsRecording;
using GrobExp.Mutators.MutatorsRecording.AssignRecording;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class NullifyIfConfiguration : AutoEvaluatorConfiguration
    {
        public NullifyIfConfiguration(Type converterType, Type type, LambdaExpression condition)
            : base(type)
        {
            ConverterType = converterType;
            Condition = condition;
        }

        public Type ConverterType { get; }

        public LambdaExpression Condition { get; }

        public override string ToString() => "nullifiedIf" + (Condition == null ? "" : "(" + Condition + ")");

        public static NullifyIfConfiguration Create<TData>(Type converterType, Expression<Func<TData, bool?>> condition)
        {
            return new NullifyIfConfiguration(converterType, typeof(TData), Prepare(condition));
        }

        internal override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new NullifyIfConfiguration(ConverterType, path.Parameters.Single().Type, path.Merge(Condition));
            // ReSharper restore ConvertClosureToMethodGroup
        }

        internal override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new NullifyIfConfiguration(ConverterType, to, Resolve(path, performer, Condition));
        }

        internal override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new NullifyIfConfiguration(ConverterType, Type, resolver.Resolve(Condition));
        }

        internal override MutatorConfiguration If(LambdaExpression condition)
        {
            return new NullifyIfConfiguration(ConverterType, Type, Prepare(condition).AndAlso(Condition));
        }

        internal override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
        }

        internal override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (Condition == null) return null;
            var infoToLog = new AssignLogInfo(path, Expression.Constant(ToString(), typeof(string)));
            var condition = Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            path = PrepareForAssign(path);
            var applyResult = Expression.IfThen(condition, Expression.Assign(path, Expression.Constant(path.Type.GetDefaultValue(), path.Type)));
            if (MutatorsAssignRecorder.IsRecording())
            {
                MutatorsAssignRecorder.RecordCompilingExpression(ConverterType, infoToLog);
                return Expression.Block(Expression.Call(RecordingMethods.RecordExecutingExpressionMethodInfo, Expression.Constant(ConverterType, typeof(Type)), Expression.Constant(infoToLog)), applyResult);
            }
            return applyResult;
        }

        protected internal override LambdaExpression[] GetDependencies()
        {
            return Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type));
        }
    }
}