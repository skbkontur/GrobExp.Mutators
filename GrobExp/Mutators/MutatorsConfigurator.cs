using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class MutatorsConfigurator<TRoot>
    {
        public MutatorsConfigurator(ModelConfigurationNode root, LambdaExpression condition = null)
        {
            this.root = root;
            Condition = condition;
        }

        public void SetMutator<TValue>(Expression<Func<TRoot, TValue>> pathToValue, MutatorConfiguration mutator)
        {
            root.Traverse(pathToValue.Body.ResolveInterfaceMembers(), true).AddMutator(mutator.If(Condition));
        }

        public void SetMutator(LambdaExpression pathToValue, MutatorConfiguration mutator)
        {
            root.Traverse(pathToValue.Body.ResolveInterfaceMembers(), true).AddMutator(mutator.If(Condition));
        }

        public MutatorsConfigurator<TRoot, TRoot, TValue> Target<TValue>(Expression<Func<TRoot, TValue>> pathToValue, StaticMultiLanguageTextBase title = null)
        {
            return new MutatorsConfigurator<TRoot, TRoot, TValue>(root, data => data, pathToValue, Condition, title);
        }

        public MutatorsConfigurator<TRoot, TRoot, TValue> Targets<TValue>(StaticMultiLanguageTextBase title = null)
        {
            return new MutatorsConfigurator<TRoot, TRoot, TValue>(root, data => data, CollectTargets<TValue>(), Condition, title);
        }

        public MutatorsConfigurator<TRoot, TChild, TChild> GoTo<TChild>(Expression<Func<TRoot, TChild>> pathToChild)
        {
            return new MutatorsConfigurator<TRoot, TChild, TChild>(root, pathToChild, pathToChild, Condition, null);
        }

        public MutatorsConfigurator<TRoot> If(Expression<Func<TRoot, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot>(root, Condition.AndAlso((LambdaExpression)new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(condition)));
        }

        public LambdaExpression Condition { get; private set; }

        protected readonly ModelConfigurationNode root;

        private static Expression<Func<TRoot, TValue[]>> CollectTargets<TValue>()
        {
            var root = Expression.Parameter(typeof(TRoot), "root");
            var targets = new List<Expression>();
            CollectTargets<TValue>(root, targets);
            return Expression.Lambda<Func<TRoot, TValue[]>>(Expression.NewArrayInit(typeof(TValue), targets), root);
        }

        private static void CollectTargets<TValue>(Expression path, List<Expression> targets)
        {
            if(path.Type == typeof(TValue))
            {
                targets.Add(path);
                return;
            }
            var properties = path.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach(var property in properties)
            {
                Expression nextPath = Expression.MakeMemberAccess(path, property);
                if(property.PropertyType.IsArray)
                    nextPath = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(property.PropertyType.GetElementType()), nextPath);
                CollectTargets<TValue>(nextPath, targets);
            }
        }
    }

    public class MutatorsConfigurator<TRoot, TChild, TValue>
    {
        public MutatorsConfigurator(ModelConfigurationNode root, Expression<Func<TRoot, TChild>> pathToChild, Expression<Func<TRoot, TValue>> pathToValue, LambdaExpression condition, StaticMultiLanguageTextBase title)
        {
            this.root = root;
            Title = title;
            PathToChild = pathToChild;
            PathToValue = pathToValue;
            Condition = condition;
        }

        public MutatorsConfigurator(ModelConfigurationNode root, Expression<Func<TRoot, TChild>> pathToChild, Expression<Func<TRoot, TValue[]>> pathToValues, LambdaExpression condition, StaticMultiLanguageTextBase title)
        {
            this.root = root;
            Title = title;
            PathToChild = pathToChild;
            PathToValues = pathToValues;
            Condition = condition;
        }

        public void SetMutator(MutatorConfiguration mutator)
        {
            MutatorConfiguration rootMutator;
            if(mutator.Type == typeof(TRoot))
                rootMutator = mutator;
            else
            {
                var pathToChild = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(PathToChild).ResolveInterfaceMembers();
                rootMutator = mutator.ToRoot((Expression<Func<TRoot, TChild>>)pathToChild);
            }
            root.Traverse(PathToValue.Body.ResolveInterfaceMembers(), true).AddMutator(rootMutator.If(Condition));
        }

        public void SetMutator(Expression<Func<TRoot, TValue>> pathToValue, MutatorConfiguration mutator)
        {
            root.Traverse(pathToValue.Body.ResolveInterfaceMembers(), true).AddMutator(mutator.If(Condition));
        }

        public MutatorsConfigurator<TRoot, T, T> GoTo<T>(Expression<Func<TChild, T>> path)
        {
            var pathToChild = PathToChild.Merge(path);
            return new MutatorsConfigurator<TRoot, T, T>(root, pathToChild, pathToChild, Condition, Title);
        }

        public MutatorsConfigurator<TRoot, TChild, T> Target<T>(Expression<Func<TValue, T>> path, StaticMultiLanguageTextBase title = null)
        {
            return new MutatorsConfigurator<TRoot, TChild, T>(root, PathToChild, PathToValue.Merge(path), Condition, title);
        }

        public MutatorsConfigurator<TRoot, TChild, TValue> If(Expression<Func<TChild, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot, TChild, TValue>(root, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression)new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(PathToChild.Merge(condition))), Title);
        }

        public MutatorsConfigurator<TRoot, TChild, TValue> IfFromRoot(Expression<Func<TRoot, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot, TChild, TValue>(root, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression)new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(condition)), Title);
        }

        public Expression<Func<TRoot, TChild>> PathToChild { get; private set; }

        public Expression<Func<TRoot, TValue>> PathToValue { get; private set; }
        public Expression<Func<TRoot, TValue[]>> PathToValues { get; private set; }
        public LambdaExpression Condition { get; set; }
        public StaticMultiLanguageTextBase Title { get; private set; }
        protected readonly ModelConfigurationNode root;
    }
}