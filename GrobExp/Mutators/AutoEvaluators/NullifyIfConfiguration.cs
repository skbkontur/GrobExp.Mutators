using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.MutatorsRecording.AssignRecording;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class NullifyIfConfiguration : AutoEvaluatorConfiguration
    {
        public NullifyIfConfiguration(Type type, LambdaExpression condition)
            : base(type)
        {
            Condition = condition;
        }

        public override string ToString()
        {
            return "nullifiedIf" + (Condition == null ? "" : "(" + Condition + ")");
        }

        public static NullifyIfConfiguration Create<TData>(Expression<Func<TData, bool?>> condition)
        {
            return new NullifyIfConfiguration(typeof(TData), Prepare(condition));
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new NullifyIfConfiguration(path.Parameters.Single().Type, path.Merge(Condition));
            // ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new NullifyIfConfiguration(to, Resolve(path, performer, Condition));
        }

        public override MutatorConfiguration ResolveAliases(LambdaAliasesResolver resolver)
        {
            return new NullifyIfConfiguration(Type, resolver.Resolve(Condition));
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new NullifyIfConfiguration(Type, Prepare(condition).AndAlso(Condition));
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
        }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (Condition == null) return null;
            var infoToLog = new AssignLogInfo(path, Expression.Constant(ToString(), typeof(string)));
            var condition = Expression.Equal(Expression.Convert(Condition.Body.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            path = PrepareForAssign(path);
            if (MutatorsAssignRecorder.IsRecording())
                MutatorsAssignRecorder.RecordCompilingExpression(infoToLog);
            return Expression.Block(
                Expression.Call(typeof(MutatorsAssignRecorder).GetMethod("RecordExecutingExpression"), Expression.Constant(infoToLog)),
                Expression.IfThen(condition, Expression.Assign(path, Expression.Constant(path.Type.GetDefaultValue(), path.Type)))
            );
        }

        public LambdaExpression Condition { get; private set; }

        protected override LambdaExpression[] GetDependencies()
        {
            return Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type));
        }
    }
}