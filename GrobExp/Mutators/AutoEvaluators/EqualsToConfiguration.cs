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
            return MakeAssignment(path, value);
        }

        protected static Expression MakeAssignment(Expression path, Expression value)
        {
            if(path.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = (MemberExpression)path;
                if(memberExpression.Expression.Type.IsArray && memberExpression.Member.Name == "Length")
                {
                    var temp = Expression.Variable(memberExpression.Expression.Type);
                    return Expression.Block(new[] {temp},
                                            Expression.Assign(temp, memberExpression.Expression),
                                            Expression.Call(arrayResizeMethod.MakeGenericMethod(memberExpression.Expression.Type.GetElementType()), temp, value),
                                            Expression.Assign(memberExpression.Expression, temp));
                }
            }
            return Expression.Assign(PrepareForAssign(path), value);
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

        private static readonly MethodInfo arrayResizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
    }
}