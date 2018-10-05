using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Aggregators;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Validators.Texts;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class MutatorsConfiguratorExtensions
    {
        public static MutatorsConfigurator<TRoot, TChild, TValue> Required<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            configurator.SetMutator(RequiredIfConfiguration.Create(MutatorsCreator.Sharp, priority, null, pathToValue, pathToChild.Merge(message), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> Required<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            configurator.SetMutator(RequiredIfConfiguration.Create(MutatorsCreator.Sharp, priority, null, pathToValue, message.Merge(pathToChild, pathToValue), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> Required<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            StaticMultiLanguageTextBase title,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.Required(child => new ValueRequiredText {Title = title}, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> Required<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.Required(child => new ValueRequiredText {Title = configurator.Title}, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> RequiredIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            configurator.SetMutator(RequiredIfConfiguration.Create(MutatorsCreator.Sharp, priority, pathToChild.Merge(condition), pathToValue, pathToChild.Merge(message), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> RequiredIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            Expression<Func<TChild, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            configurator.SetMutator(RequiredIfConfiguration.Create(MutatorsCreator.Sharp, priority, pathToChild.Merge(condition), pathToValue, message.Merge(pathToChild, pathToValue), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> RequiredIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            StaticMultiLanguageTextBase title,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.RequiredIf(condition, child => new ValueRequiredText {Title = title}, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> RequiredIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.RequiredIf(condition, child => new ValueRequiredText {Title = configurator.Title}, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, string> IsLike<TRoot, TChild>(
            this MutatorsConfigurator<TRoot, TChild, string> configurator,
            string pattern,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, string>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            configurator.SetMutator(RegexValidatorConfiguration.Create(MutatorsCreator.Sharp, priority, pathToValue, null, pathToChild.Merge(message), pattern, type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, string> IsLike<TRoot, TChild>(
            this MutatorsConfigurator<TRoot, TChild, string> configurator,
            string pattern,
            Expression<Func<TChild, string, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, string>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            configurator.SetMutator(RegexValidatorConfiguration.Create(MutatorsCreator.Sharp, priority, pathToValue, null, message.Merge(pathToChild, pathToValue), pattern, type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, string> IsLike<TRoot, TChild>(
            this MutatorsConfigurator<TRoot, TChild, string> configurator,
            string pattern,
            StaticMultiLanguageTextBase title,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.IsLike(pattern, child => new ValueShouldMatchPatternText {Title = title, Pattern = pattern}, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, string> IsLike<TRoot, TChild>(
            this MutatorsConfigurator<TRoot, TChild, string> configurator,
            string pattern,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.IsLike(pattern, child => new ValueShouldMatchPatternText {Title = configurator.Title, Pattern = pattern}, priority, type);
        }

        public static void FilledExactlyOneOf<TRoot, TChild>(
            this MutatorsConfigurator<TRoot, TChild, TChild> configurator,
            Expression<Func<TChild, object[]>> targets,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            Expression sum = null;
            if (targets.Body.NodeType != ExpressionType.NewArrayInit)
                throw new InvalidOperationException("Expected new array creation");
            Expression lcp = null;
            foreach (var expression in ((NewArrayExpression)targets.Body).Expressions)
            {
                var target = expression.NodeType == ExpressionType.Convert ? ((UnaryExpression)expression).Operand : expression;
                if (target.Type.IsValueType && !IsNullable(target.Type))
                    throw new InvalidOperationException("Type '" + target.Type + "' cannot be null");
                lcp = lcp == null ? target : lcp.LCP(target);
                Expression current = Expression.Condition(Expression.Equal(target, Expression.Constant(null, target.Type)), Expression.Constant(0), Expression.Constant(1));
                sum = sum == null ? current : Expression.Add(sum, current);
            }

            if (sum == null) return;
            Expression condition = Expression.NotEqual(sum, Expression.Constant(1));
            configurator.SetMutator(configurator.PathToChild.Merge(Expression.Lambda(lcp, targets.Parameters)).Body, configurator.PathToChild.Merge(targets).Body, InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, configurator.PathToChild.Merge(Expression.Lambda<Func<TChild, bool?>>(Expression.Convert(condition, typeof(bool?)), targets.Parameters)), configurator.PathToChild.Merge(message), type));
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> InvalidIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, condition, message, type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> InvalidIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            Expression<Func<TChild, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, pathToChild.Merge(condition), message.Merge(pathToChild, pathToValue), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> InvalidIfFromRoot<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TRoot, bool?>> condition,
            Expression<Func<TRoot, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, condition, message, type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, TValue>> expectedValue,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var condition = Expression.Convert(Expression.NotEqual(pathToValue.ReplaceParameter(pathToChild.Parameters[0]).Body, pathToChild.Merge(expectedValue).Body), typeof(bool?));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToChild.Parameters), pathToChild.Merge(message), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TRoot, TChild, TValue>> expectedValue,
            Expression<Func<TRoot, TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var rootParameter = pathToChild.Parameters[0];
            var root = Expression.Lambda<Func<TRoot, TRoot>>(rootParameter, rootParameter);
            var condition = Expression.Convert(Expression.NotEqual(pathToValue.ReplaceParameter(rootParameter).Body, expectedValue.Merge(root, pathToChild).Body), typeof(bool?));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToChild.Parameters), message.Merge(root, pathToChild), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, TValue>> expectedValue,
            IEqualityComparer<TValue> comparer = null,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var pathToValue = ((Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue)).ReplaceParameter(pathToChild.Parameters[0]).Body;
            var equal = comparer == null
                            ? (Expression)Expression.Equal(pathToValue, pathToChild.Merge(expectedValue).Body)
                            : Expression.Call(Expression.Constant(comparer, typeof(IEqualityComparer<TValue>)), "Equals", Type.EmptyTypes, pathToValue, pathToChild.Merge(expectedValue).Body);
            var condition = Expression.Convert(Expression.Not(equal), typeof(bool?));
            var message = Expression.MemberInit(Expression.New(typeof(ValueMustBeEqualToText)),
                                                Expression.Bind(valueMustBeEqualToTextExpectedValueProperty, Expression.Convert(expectedValue.Body, typeof(object))),
                                                Expression.Bind(valueMustBeEqualToTextActualValueProperty, Expression.Convert(pathToValue, typeof(object))));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToChild.Parameters), pathToChild.Merge(Expression.Lambda<Func<TChild, MultiLanguageTextBase>>(message, expectedValue.Parameters)), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            TValue expectedValue,
            IEqualityComparer<TValue> comparer = null,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.MustBeEqualTo(child => expectedValue, comparer, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, TValue>> expectedValue,
            Expression<Func<TChild, TValue, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var pathToExpectedValue = pathToChild.Merge(expectedValue);
            var condition = Expression.Convert(Expression.NotEqual(pathToValue.ReplaceParameter(pathToChild.Parameters[0]).Body, pathToExpectedValue.Body), typeof(bool?));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToChild.Parameters), message.Merge(pathToChild, pathToValue, pathToExpectedValue), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, TValue>> expectedValue,
            Expression<Func<TValue, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var pathToExpectedValue = pathToChild.Merge(expectedValue);
            var condition = Expression.Convert(Expression.NotEqual(pathToValue.ReplaceParameter(pathToChild.Parameters[0]).Body, pathToExpectedValue.Body), typeof(bool?));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToChild.Parameters), message.Merge(pathToValue, pathToExpectedValue), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            TValue expectedValue,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.MustBeEqualTo(child => expectedValue, message, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            TValue expectedValue,
            Expression<Func<TChild, TValue, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.MustBeEqualTo(child => expectedValue, message, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBeEqualTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            TValue expectedValue,
            Expression<Func<TValue, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.MustBeEqualTo(child => expectedValue, message, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBelongTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            IEnumerable<TValue> values,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var contains = Expression.Call(containsMethod.MakeGenericMethod(typeof(TValue)), Expression.Constant(values), pathToValue.Body);
            var condition = Expression.Convert(Expression.Not(contains), typeof(bool?));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToValue.Parameters), pathToChild.Merge(message), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBelongTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            IEnumerable<TValue> values,
            IEqualityComparer<TValue> comparer = null,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            Expression contains = comparer == null
                                      ? Expression.Call(containsMethod.MakeGenericMethod(typeof(TValue)), Expression.Constant(values), pathToValue.Body)
                                      : Expression.Call(containsWithComparerMethod.MakeGenericMethod(typeof(TValue)), Expression.Constant(values), pathToValue.Body, Expression.Constant(comparer));
            var condition = Expression.Convert(Expression.Not(contains), typeof(bool?));
            var message = Expression.MemberInit(Expression.New(typeof(ValueMustBelongToText)),
                                                Expression.Bind(typeof(ValueMustBelongToText).GetMember("Value").Single(), pathToValue.Body),
                                                Expression.Bind(typeof(ValueMustBelongToText).GetMember("Values").Single(), Expression.Constant(values.Cast<object>().ToArray())));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToValue.Parameters), Expression.Lambda<Func<TRoot, MultiLanguageTextBase>>(message, pathToChild.Parameters), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> MustBelongTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            IEnumerable<TValue> values,
            Expression<Func<TChild, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var contains = Expression.Call(containsMethod.MakeGenericMethod(typeof(TValue)), Expression.Constant(values), pathToValue.Body);
            var condition = Expression.Convert(Expression.Not(contains), typeof(bool?));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToValue.Parameters), message.Merge(pathToChild, pathToValue), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> IfFilledMustBelongTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            IEnumerable<TValue> values,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var contains = Expression.Call(containsMethod.MakeGenericMethod(typeof(TValue)), Expression.Constant(values), pathToValue.Body);
            var condition = Expression.Convert(Expression.AndAlso(Expression.NotEqual(pathToValue.Body, Expression.Constant(null, typeof(TValue))), Expression.Not(contains)), typeof(bool?));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToValue.Parameters), pathToChild.Merge(message), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> IfFilledMustBelongTo<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            IEnumerable<TValue> values,
            Expression<Func<TChild, TValue, MultiLanguageTextBase>> message,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, TValue>>)methodReplacer.Visit(configurator.PathToValue);
            var pathToChild = (Expression<Func<TRoot, TChild>>)methodReplacer.Visit(configurator.PathToChild);
            var contains = Expression.Call(containsMethod.MakeGenericMethod(typeof(TValue)), Expression.Constant(values), pathToValue.Body);
            var condition = Expression.Convert(Expression.AndAlso(Expression.NotEqual(pathToValue.Body, Expression.Constant(null, typeof(TValue))), Expression.Not(contains)), typeof(bool?));
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToValue.Parameters), message.Merge(pathToChild, pathToValue), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, string> NotLongerThan<TRoot, TChild>(
            this MutatorsConfigurator<TRoot, TChild, string> configurator,
            int length,
            MultiLanguageTextBase title,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, string>>)methodReplacer.Visit(configurator.PathToValue);
            var condition = Expression.Convert(Expression.GreaterThan(Expression.MakeMemberAccess(pathToValue.Body, stringLengthProperty), Expression.Constant(length)), typeof(bool?));
            var message = Expression.Lambda<Func<TRoot, MultiLanguageTextBase>>(Expression.MemberInit(
                Expression.New(typeof(LengthOutOfRangeText)),
                Expression.Bind(lengthOutOfRangeTextToProperty, Expression.Constant(length, typeof(int?))),
                Expression.Bind(lengthOutOfRangeTextTitleProperty, Expression.Constant(title, typeof(MultiLanguageTextBase)))), pathToValue.Parameters);
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToValue.Parameters), message, type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, string> NotLongerThan<TRoot, TChild>(
            this MutatorsConfigurator<TRoot, TChild, string> configurator,
            int length,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.NotLongerThan(length, configurator.Title, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, string> LengthInRange<TRoot, TChild>(
            this MutatorsConfigurator<TRoot, TChild, string> configurator,
            int fromLength,
            int toLength,
            MultiLanguageTextBase title,
            int priority = 0,
            ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, string>>)methodReplacer.Visit(configurator.PathToValue);
            var leftExpression = Expression.LessThan(Expression.MakeMemberAccess(pathToValue.Body, stringLengthProperty), Expression.Constant(fromLength));
            var rigthExpression = Expression.GreaterThan(Expression.MakeMemberAccess(pathToValue.Body, stringLengthProperty), Expression.Constant(toLength));

            var condition = Expression.Convert(Expression.OrElse(leftExpression, rigthExpression), typeof(bool?));
            var message = Expression.Lambda<Func<TRoot, MultiLanguageTextBase>>(Expression.MemberInit(
                Expression.New(typeof(LengthOutOfRangeText)),
                Expression.Bind(lengthOutOfRangeTextToProperty, Expression.Constant(toLength, typeof(int?))),
                Expression.Bind(lengthOutOfRangeTextFromProperty, Expression.Constant(fromLength, typeof(int?))),
                Expression.Bind(lengthOutOfRangeTextTitleProperty, Expression.Constant(title, typeof(MultiLanguageTextBase)))), pathToValue.Parameters);

            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToValue.Parameters), message, type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, string> LengthInRange<TRoot, TChild>(this MutatorsConfigurator<TRoot, TChild, string> configurator, int fromLength, int toLength,
                                                                                               int priority = 0,
                                                                                               ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.LengthInRange(fromLength, toLength, configurator.Title, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, string> LengthExactly<TRoot, TChild>(this MutatorsConfigurator<TRoot, TChild, string> configurator, int length, MultiLanguageTextBase title,
                                                                                               int priority = 0,
                                                                                               ValidationResultType type = ValidationResultType.Error)
        {
            var methodReplacer = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod);
            var pathToValue = (Expression<Func<TRoot, string>>)methodReplacer.Visit(configurator.PathToValue);
            var condition = Expression.Convert(Expression.NotEqual(Expression.MakeMemberAccess(pathToValue.Body, stringLengthProperty), Expression.Constant(length)), typeof(bool?));
            var message = Expression.Lambda<Func<TRoot, MultiLanguageTextBase>>(Expression.MemberInit(
                Expression.New(typeof(LengthNotExactlyEqualsText)),
                Expression.Bind(lengthNotExactlyEqualsTextExacltyProperty, Expression.Constant(length, typeof(int?))),
                Expression.Bind(lengthNotExactlyEqualsTextTitleProperty, Expression.Constant(title, typeof(MultiLanguageTextBase))),
                Expression.Bind(lengthNotExactlyEqualsTextValueProperty, pathToValue.Body)), pathToValue.Parameters);
            configurator.SetMutator(InvalidIfConfiguration.Create(MutatorsCreator.Sharp, priority, Expression.Lambda<Func<TRoot, bool?>>(condition, pathToValue.Parameters), message, type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, string> LengthExactly<TRoot, TChild>(this MutatorsConfigurator<TRoot, TChild, string> configurator, int length,
                                                                                               int priority = 0,
                                                                                               ValidationResultType type = ValidationResultType.Error)
        {
            return configurator.LengthExactly(length, configurator.Title, priority, type);
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> NullifyIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition)
        {
            configurator.SetMutator(NullifyIfConfiguration.Create(configurator.Root.ConfiguratorType, condition));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> DisabledIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition)
        {
            configurator.SetMutator(DisableIfConfiguration.Create(condition));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> HiddenIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition)
        {
            configurator.SetMutator(HideIfConfiguration.Create(condition));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> Set<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, TValue>> value)
        {
            configurator.SetMutator(EqualsToConfiguration.Create(configurator.Root.ConfiguratorType, typeof(TChild), value, null));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> Set<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            TValue value)
        {
            configurator.Set(child => value);
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> SetIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            Expression<Func<TChild, TValue>> value)
        {
            configurator.SetMutator(EqualsToIfConfiguration.Create(configurator.Root.ConfiguratorType, typeof(TChild), condition, value, null));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue[]> SetArrayLength<TRoot, TChild, TValue>(this MutatorsConfigurator<TRoot, TChild, TValue[]> configurator, Expression<Func<TChild, int>> length)
        {
            configurator.SetMutator(SetArrayLengthConfiguration.Create(null, configurator.PathToChild.Merge(length)));
            return configurator;
        }

        private static bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static readonly MemberInfo valueMustBeEqualToTextExpectedValueProperty = ((MemberExpression)((Expression<Func<ValueMustBeEqualToText, object>>)(text => text.ExpectedValue)).Body).Member;
        private static readonly MemberInfo valueMustBeEqualToTextActualValueProperty = ((MemberExpression)((Expression<Func<ValueMustBeEqualToText, object>>)(text => text.ActualValue)).Body).Member;
        private static readonly MemberInfo valueMustBeEqualToTextTitleProperty = ((MemberExpression)((Expression<Func<ValueMustBeEqualToText, MultiLanguageTextBase>>)(text => text.Title)).Body).Member;

        private static readonly MethodInfo containsMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, bool>>)(enumerable => enumerable.Contains(0))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo containsWithComparerMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<string>, bool>>)(enumerable => enumerable.Contains("", StringComparer.CurrentCulture))).Body).Method.GetGenericMethodDefinition();

        private static readonly MemberInfo lengthNotExactlyEqualsTextExacltyProperty = ((MemberExpression)((Expression<Func<LengthNotExactlyEqualsText, int?>>)(text => text.Exactly)).Body).Member;
        private static readonly MemberInfo lengthNotExactlyEqualsTextTitleProperty = ((MemberExpression)((Expression<Func<LengthNotExactlyEqualsText, MultiLanguageTextBase>>)(text => text.Title)).Body).Member;
        private static readonly MemberInfo lengthNotExactlyEqualsTextValueProperty = ((MemberExpression)((Expression<Func<LengthNotExactlyEqualsText, object>>)(text => text.Value)).Body).Member;

        private static readonly MemberInfo lengthOutOfRangeTextToProperty = ((MemberExpression)((Expression<Func<LengthOutOfRangeText, int?>>)(text => text.To)).Body).Member;
        private static readonly MemberInfo lengthOutOfRangeTextFromProperty = ((MemberExpression)((Expression<Func<LengthOutOfRangeText, int?>>)(text => text.From)).Body).Member;
        private static readonly MemberInfo lengthOutOfRangeTextTitleProperty = ((MemberExpression)((Expression<Func<LengthOutOfRangeText, MultiLanguageTextBase>>)(text => text.Title)).Body).Member;

        private static readonly PropertyInfo stringLengthProperty = (PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member;
    }
}