using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class ConverterConfiguratorExtensions
    {
        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> Set<TSourceRoot, TSourceChild, TSourceValue, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TSourceChild, TSourceValue>> value,
            Expression<Func<TSourceValue, TDestValue>> converter,
            Expression<Func<TSourceValue, ValidationResult>> validator,
            int priority = 0)
        {
            Expression<Func<TSourceRoot, TSourceValue>> valueFromRoot = configurator.PathToSourceChild.Merge(value);
            LambdaExpression convertedValue = converter == null ? (LambdaExpression)valueFromRoot : valueFromRoot.Merge(converter);
            StaticValidatorConfiguration validatorConfiguration = validator == null ? null : StaticValidatorConfiguration.Create(priority, null, valueFromRoot, validator);
            configurator.SetMutator(EqualsToConfiguration.Create(typeof(TDestRoot), convertedValue, validatorConfiguration));
            return configurator;
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> Set<TSourceRoot, TSourceChild, TSourceValue, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TSourceChild, TSourceValue>> value,
            Expression<Func<TSourceValue, TDestValue>> converter,
            Expression<Func<TSourceValue, bool?>> validator = null,
            Expression<Func<TSourceValue, MultiLanguageTextBase>> message = null,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            if(validator == null)
                return configurator.Set(value, converter, null, priority);
            Expression test = Expression.Equal(validator.Body, Expression.Constant(true, typeof(bool?)));
            Expression ifTrue = Expression.New(validationResultConstructor, Expression.Constant(type), Expression.Lambda(validator.Parameters[0], validator.Parameters[0]).Merge(message).Body);
            Expression ifFalse = Expression.Constant(ValidationResult.Ok);
            return configurator.Set(value, converter, Expression.Lambda<Func<TSourceValue, ValidationResult>>(Expression.Condition(test, ifTrue, ifFalse), validator.Parameters), priority);
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget> Set<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget> configurator,
            Expression<Func<TSourceChild, TDestChild, TTarget>> value)
        {
            LambdaExpression valueFromRoot = new ExpressionMerger(configurator.PathToSourceChild, configurator.PathToChild).Merge(value);
            configurator.SetMutator(EqualsToConfiguration.Create(typeof(TDestRoot), valueFromRoot, null));
            return configurator;
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget> Set<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget> configurator,
            Expression<Func<TSourceChild, TTarget>> value)
        {
            LambdaExpression valueFromRoot = configurator.PathToSourceChild.Merge(value);
            configurator.SetMutator(EqualsToConfiguration.Create(typeof(TDestRoot), valueFromRoot, null));
            return configurator;
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TValue> Set<TSourceRoot, TSourceChild, TValue, TDestRoot, TDestChild>(this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TValue> configurator, TValue value)
        {
            return configurator.Set(child => value);
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> NullifyIf<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TDestChild, bool?>> condition)
        {
            configurator.SetMutator(NullifyIfConfiguration.Create(condition));
            return configurator;
        }

        public static void BatchSet<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TDestValue, TSourceChild, Batch>> batch)
        {
            var merger = new ExpressionMerger(configurator.PathToSourceChild);
            var initializers = ((ListInitExpression)batch.Body).Initializers;
            Expression primaryKeyIsEmpty = null;
            foreach(var initializer in initializers)
            {
                Expression dest = initializer.Arguments[0];
                var clearedDest = ClearNotNull(dest);
                if(clearedDest != null)
                {
                    var current = Expression.Equal(clearedDest, Expression.Constant(null, clearedDest.Type));
                    primaryKeyIsEmpty = primaryKeyIsEmpty == null ? current : Expression.AndAlso(primaryKeyIsEmpty, current);
                }
                dest = clearedDest ?? dest;
                if(dest.Type != typeof(object))
                    dest = Expression.Convert(dest, typeof(object));
                Expression source = initializer.Arguments[1];
                if(source.Type != typeof(object))
                    source = Expression.Convert(source, typeof(object));
                LambdaExpression value = merger.Merge(Expression.Lambda(source, batch.Parameters[1]));
                configurator.Target(Expression.Lambda<Func<TDestValue, object>>(dest, batch.Parameters[0])).SetMutator(EqualsToConfiguration.Create(typeof(TDestRoot), value, null));
            }
            if(primaryKeyIsEmpty == null) return;
            var condition = (Expression<Func<TDestRoot, bool?>>)configurator.PathToChild.Merge(Expression.Lambda(Expression.Convert(primaryKeyIsEmpty, typeof(bool?)), batch.Parameters[0]));
            foreach(var initializer in initializers)
            {
                Expression dest = initializer.Arguments[0];
                if(ClearNotNull(dest) != null)
                    continue;
                if(dest.Type != typeof(object))
                    dest = Expression.Convert(dest, typeof(object));
                configurator.Target(Expression.Lambda<Func<TDestValue, object>>(dest, batch.Parameters[0])).SetMutator(NullifyIfConfiguration.Create(condition));
            }
        }

        private static Expression ClearNotNull(Expression path)
        {
            while(path.NodeType == ExpressionType.Convert)
                path = ((UnaryExpression)path).Operand;
            if(path.NodeType == ExpressionType.Call)
            {
                var methodCallExpression = (MethodCallExpression)path;
                if(methodCallExpression.Method.IsNotNullMethod())
                    return methodCallExpression.Arguments.Single();
            }
            return null;
        }

        private static readonly ConstructorInfo validationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult>>)(() => new ValidationResult(ValidationResultType.Ok, null))).Body).Constructor;
    }
}