using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class EqualsToIfConfiguration : EqualsToConfiguration
    {
        public EqualsToIfConfiguration(Type type, LambdaExpression condition, LambdaExpression value, StaticValidatorConfiguration validator)
            : base(type, value, validator)
        {
            Condition = condition;
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            base.GetArrays(arraysExtractor);
            arraysExtractor.GetArrays(Condition);
        }

        public static EqualsToIfConfiguration Create(Type type, LambdaExpression condition, LambdaExpression value, StaticValidatorConfiguration validator)
        {
            return new EqualsToIfConfiguration(type, Prepare(condition), Prepare(value), validator);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new EqualsToIfConfiguration(path.Parameters.Single().Type, path.Merge(Condition), path.Merge(Value), Validator);
            // ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            if(Validator != null)
                throw new NotSupportedException();
            return new EqualsToIfConfiguration(to, Resolve(path, performer, Condition), Resolve(path, performer, Value), Validator);
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new EqualsToIfConfiguration(Type, Prepare(condition).AndAlso(Condition), Value, Validator == null ? null : (StaticValidatorConfiguration)Validator.If(condition));
        }

        public override MutatorConfiguration ResolveAliases(AliasesResolver resolver)
        {
            return new EqualsToIfConfiguration(Type, (LambdaExpression)resolver.Visit(Condition), (LambdaExpression)resolver.Visit(Value), Validator == null ? null : (StaticValidatorConfiguration)Validator.ResolveAliases(resolver));
        }

        protected override Expression GetLCP()
        {
             return (Condition == null ? new Expression[0] : Condition.Body.CutToChains(false, false)).Concat(Value == null ? new Expression[0] : Value.Body.CutToChains(false, false)).FindLCP();
       }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if(Value == null) return null;
            path = PrepareForAssign(path);
            var value = Convert(Value.Body.ResolveAliases(aliases), path.Type);
            var assignment = path.Assign(value);
            if(Condition == null)
                return assignment;
            var condition = Condition.Body;
            condition = Expression.Equal(Expression.Convert(condition.ResolveAliases(aliases), typeof(bool?)), Expression.Constant(true, typeof(bool?)));
            return Expression.IfThen(condition, assignment);
        }

        public LambdaExpression Condition { get; private set; }

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