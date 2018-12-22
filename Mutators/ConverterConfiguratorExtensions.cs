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
        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> Set<TSourceRoot, TSourceChild, TSourceNode, TSourceValue, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TSourceChild, TSourceNode>> node,
            Expression<Func<TSourceNode, TSourceValue>> value,
            Expression<Func<TSourceValue, TDestValue>> converter,
            Expression<Func<TSourceNode, ValidationResult>> validator,
            int priority = 0)
        {
            var pathToSourceChild = (Expression<Func<TSourceRoot, TSourceChild>>)configurator.PathToSourceChild.ReplaceEachWithCurrent();
            var nodeFromRoot = pathToSourceChild.Merge(node);
            var valueFromRoot = nodeFromRoot.Merge(value);
            var convertedValue = converter == null ? (LambdaExpression)valueFromRoot : valueFromRoot.Merge(converter);
            var validatorConfiguration = validator == null
                                             ? null
                                             : StaticValidatorConfiguration.Create(MutatorsCreator.Sharp, "SetWithValidator", priority,
                                                                                   null, nodeFromRoot, valueFromRoot, validator);
            configurator.SetMutator(EqualsToConfiguration.Create(configurator.Root.ConfiguratorType, typeof(TDestRoot), convertedValue, validatorConfiguration));
            return configurator;
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> Set<TSourceRoot, TSourceChild, TSourceValue, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TSourceChild, TSourceValue>> value,
            Expression<Func<TSourceValue, TDestValue>> converter,
            Expression<Func<TSourceValue, ValidationResult>> validator,
            int priority = 0)
        {
            return configurator.Set(value, x => x, converter, validator, priority);
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> Set<TSourceRoot, TSourceChild, TSourceValue, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TSourceChild, TSourceValue>> value,
            Expression<Func<TSourceValue, TDestValue>> converter)
        {
            return configurator.Set(value, converter, null, 0);
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> Set<TSourceRoot, TSourceChild, TSourceValue, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TSourceChild, TSourceValue>> value,
            Expression<Func<TSourceValue, TDestValue>> converter,
            Expression<Func<TSourceValue, bool?>> validator,
            Expression<Func<TSourceValue, MultiLanguageTextBase>> message = null,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            Expression test = Expression.Equal(validator.Body, Expression.Constant(true, typeof(bool?)));
            Expression ifTrue = Expression.New(validationResultConstructor, Expression.Constant(type), Expression.Lambda(validator.Parameters[0], validator.Parameters[0]).Merge(message).Body);
            Expression ifFalse = Expression.Constant(ValidationResult.Ok);
            return configurator.Set(value, converter, Expression.Lambda<Func<TSourceValue, ValidationResult>>(Expression.Condition(test, ifTrue, ifFalse), validator.Parameters), priority);
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget> Set<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget> configurator,
            Expression<Func<TSourceChild, TDestChild, TTarget>> value)
        {
            var pathToSourceChild = (Expression<Func<TSourceRoot, TSourceChild>>)configurator.PathToSourceChild.ReplaceEachWithCurrent();
            var pathToChild = (Expression<Func<TDestRoot, TDestChild>>)configurator.PathToChild.ReplaceEachWithCurrent();
            LambdaExpression valueFromRoot = new ExpressionMerger(pathToSourceChild, pathToChild).Merge(value);
            configurator.SetMutator(EqualsToConfiguration.Create(configurator.Root.ConfiguratorType, typeof(TDestRoot), valueFromRoot, null));
            return configurator;
        }

        public static ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget> Set<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget> configurator,
            Expression<Func<TSourceChild, TTarget>> value)
        {
            var pathToSourceChild = (Expression<Func<TSourceRoot, TSourceChild>>)configurator.PathToSourceChild.ReplaceEachWithCurrent();
            LambdaExpression valueFromRoot = pathToSourceChild.Merge(value);
            configurator.SetMutator(EqualsToConfiguration.Create(configurator.Root.ConfiguratorType, typeof(TDestRoot), valueFromRoot, null));
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
            configurator.SetMutator(NullifyIfConfiguration.Create(configurator.Root.ConfiguratorType, condition));
            return configurator;
        }

        public static void BatchSet<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue>(
            this ConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue> configurator,
            Expression<Func<TDestValue, TSourceChild, Batch>> batch)
        {
            var pathToSourceChild = (Expression<Func<TSourceRoot, TSourceChild>>)configurator.PathToSourceChild.ReplaceEachWithCurrent();
            var pathToChild = (Expression<Func<TDestRoot, TDestChild>>)configurator.PathToChild.ReplaceEachWithCurrent();
            var merger = new ExpressionMerger(pathToSourceChild);
            var initializers = ((ListInitExpression)batch.Body).Initializers;
            Expression primaryKeyIsEmpty = null;
            foreach (var initializer in initializers)
            {
                Expression dest = initializer.Arguments[0];
                var clearedDest = ClearNotNull(dest);
                if (clearedDest != null)
                {
                    var current = Expression.Equal(clearedDest, Expression.Constant(null, clearedDest.Type));
                    primaryKeyIsEmpty = primaryKeyIsEmpty == null ? current : Expression.AndAlso(primaryKeyIsEmpty, current);
                }

                dest = clearedDest ?? dest;
                if (dest.Type != typeof(object))
                    dest = Expression.Convert(dest, typeof(object));
                Expression source = initializer.Arguments[1].ReplaceEachWithCurrent();
//                if(source.Type != typeof(object))
//                    source = Expression.Convert(source, typeof(object));
                LambdaExpression value = merger.Merge(Expression.Lambda(source, batch.Parameters[1]));
                if (dest.NodeType == ExpressionType.Convert)
                    dest = ((UnaryExpression)dest).Operand;
                dest = pathToChild.Merge(Expression.Lambda(dest, batch.Parameters[0])).Body;
                configurator.ToRoot().SetMutator(dest, EqualsToConfiguration.Create(configurator.Root.ConfiguratorType, typeof(TDestRoot), value, null));
                //configurator.Target(Expression.Lambda<Func<TDestValue, object>>(dest, batch.Parameters[0])).SetMutator(EqualsToConfiguration.Create(typeof(TDestRoot), value, null));
            }

            if (primaryKeyIsEmpty == null) return;
            var condition = (Expression<Func<TDestRoot, bool?>>)pathToChild.Merge(Expression.Lambda(Expression.Convert(primaryKeyIsEmpty.ReplaceEachWithCurrent(), typeof(bool?)), batch.Parameters[0]));
            foreach (var initializer in initializers)
            {
                Expression dest = initializer.Arguments[0];
                if (ClearNotNull(dest) != null)
                    continue;
                if (dest.Type != typeof(object))
                    dest = Expression.Convert(dest, typeof(object));
                if (dest.NodeType == ExpressionType.Convert)
                    dest = ((UnaryExpression)dest).Operand;
                dest = pathToChild.Merge(Expression.Lambda(dest, batch.Parameters[0])).Body;
                configurator.ToRoot().SetMutator(dest, NullifyIfConfiguration.Create(configurator.Root.ConfiguratorType, condition));

                //configurator.Target(Expression.Lambda<Func<TDestValue, object>>(dest, batch.Parameters[0])).SetMutator(NullifyIfConfiguration.Create(condition));
            }
        }

        private static Expression ClearNotNull(Expression path)
        {
            while (path.NodeType == ExpressionType.Convert)
                path = ((UnaryExpression)path).Operand;
            if (path.NodeType == ExpressionType.Call)
            {
                var methodCallExpression = (MethodCallExpression)path;
                if (methodCallExpression.Method.IsNotNullMethod())
                    return methodCallExpression.Arguments.Single();
            }

            return null;
        }

        private static readonly ConstructorInfo validationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult>>)(() => new ValidationResult(ValidationResultType.Ok, null))).Body).Constructor;
    }
}