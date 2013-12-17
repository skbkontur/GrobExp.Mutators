using System;
using System.Linq.Expressions;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class MutatorsConfiguratorExtensions
    {
        public static MutatorsConfigurator<TRoot, TChild, TValue> InvalidIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            Expression<Func<TChild, MultiLanguageTextBase>> message,
            ValidationResultType type = ValidationResultType.Error)
        {
            configurator.SetMutator(InvalidIfConfiguration.Create(condition, message, type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> InvalidIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition,
            Expression<Func<TChild, TValue, MultiLanguageTextBase>> message,
            ValidationResultType type = ValidationResultType.Error)
        {
            var pathToValue = (Expression<Func<TRoot, TValue>>)new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(configurator.PathToValue);
            var pathToChild = configurator.PathToChild;
            configurator.SetMutator(InvalidIfConfiguration.Create(pathToChild.Merge(condition), message.Merge(pathToChild, pathToValue), type));
            return configurator;
        }

        public static MutatorsConfigurator<TRoot, TChild, TValue> NullifyIf<TRoot, TChild, TValue>(
            this MutatorsConfigurator<TRoot, TChild, TValue> configurator,
            Expression<Func<TChild, bool?>> condition)
        {
            configurator.SetMutator(NullifyIfConfiguration.Create(condition));
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
            configurator.SetMutator(EqualsToConfiguration.Create(typeof(TChild), value, null));
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
            configurator.SetMutator(EqualsToIfConfiguration.Create(typeof(TChild), condition, value, null));
            return configurator;
        }
    }
}