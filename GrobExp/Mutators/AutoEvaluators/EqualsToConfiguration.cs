using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.AutoEvaluators
{
    public class EqualsToConfiguration : AutoEvaluatorConfiguration
    {
        protected EqualsToConfiguration(Type type, LambdaExpression value, StaticValidatorConfiguration validator)
            : base(type)
        {
            Value = value;
            Validator = validator;
        }

        public static EqualsToConfiguration Create<TData>(LambdaExpression value, StaticValidatorConfiguration validator = null)
        {
            return new EqualsToConfiguration(typeof(TData), Prepare(value), validator);
        }

        public static EqualsToConfiguration Create(Type type, LambdaExpression value, StaticValidatorConfiguration validator)
        {
            return new EqualsToConfiguration(type, Prepare(value), validator);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            // ReSharper disable ConvertClosureToMethodGroup
            return new EqualsToConfiguration(path.Parameters.Single().Type, path.Merge(Value), Validator);
            // ReSharper restore ConvertClosureToMethodGroup
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            if(Validator != null)
                throw new NotSupportedException();
            return new EqualsToConfiguration(to, Resolve(path, performer, Value), Validator);
        }

        public override MutatorConfiguration ResolveAliases(AliasesResolver resolver)
        {
            return new EqualsToConfiguration(Type, (LambdaExpression)resolver.Visit(Value), Validator == null ? null : (StaticValidatorConfiguration)Validator.ResolveAliases(resolver));
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new EqualsToIfConfiguration(Type, Prepare(condition), Value, Validator == null ? null : (StaticValidatorConfiguration)Validator.If(condition));
        }

        public override Expression Apply(Expression path, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if(Value == null) return null;
            var value = Convert(Value.Body.ResolveAliases(aliases), path.Type);
            Expression assignment;
            if(path.NodeType != ExpressionType.Call)
                assignment = Expression.Assign(PrepareForAssign(path), value);
            else
            {
                var methodCallExpression = (MethodCallExpression)path;
                var method = methodCallExpression.Method;
                if(!method.IsIndexerGetter())
                    throw new InvalidOperationException("Cannot assign to " + path);
                if(methodCallExpression.Object.Type.IsDictionary())
                    assignment = methodCallExpression.Object.AddToDictionary(methodCallExpression.Arguments.Single(), value);
                else
                {
                    var setter = methodCallExpression.Object.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();
                    assignment = Expression.Call(methodCallExpression.Object, setter, methodCallExpression.Arguments.Concat(new[] {value}));
                }
                assignment = Expression.Block(
                    Expression.IfThen(Expression.Equal(methodCallExpression.Object, Expression.Constant(null, methodCallExpression.Object.Type)), Expression.Assign(methodCallExpression.Object, Expression.New(methodCallExpression.Object.Type))),
                    assignment);
            }
            return assignment;
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Value);
        }

        public LambdaExpression Value { get; private set; }

        public StaticValidatorConfiguration Validator { get; private set; }

        protected override LambdaExpression[] GetDependencies()
        {
            return Value == null ? new LambdaExpression[0] : Value.ExtractDependencies(Value.Parameters.Where(parameter => parameter.Type == Type));
        }

        protected override Expression GetLCP()
        {
            return Value == null ? null : Value.Body.CutToChains(false, false).FindLCP();
        }
    }
}