using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class ModelConfigurationNodeMigration
    {
        public static void Migrate(this ModelConfigurationNode theNode, Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationTree)
        {
            theNode.MigrateTree(to, destTree, convertationTree, convertationTree, theNode.Parent == null ? theNode.Path : Expression.Parameter(theNode.NodeType, theNode.NodeType.Name), false);
        }

        private static ModelConfigurationNode GoTo(ModelConfigurationNode node, ModelConfigurationEdge edge)
        {
            if(node == null)
                return null;
            ModelConfigurationNode result;
            return node.children.TryGetValue(edge, out result) ? result : null;
        }

        private static void MigrateTree(this ModelConfigurationNode node, Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationRoot, ModelConfigurationNode convertationNode, Expression path, bool zzz)
        {
            node.MigrateNode(to, destTree, convertationRoot, path);
            if(!zzz && convertationNode != null && convertationNode.mutators.Any(pair => pair.Value is EqualsToConfiguration))
                zzz = true;
            foreach(var entry in node.children)
            {
                var edge = entry.Key;
                var child = entry.Value;
                var convertationChild = GoTo(convertationNode, edge);
                if(edge.Value is int)
                {
                    if(convertationChild == null)
                        convertationChild = GoTo(convertationNode, ModelConfigurationEdge.Each);
                    child.MigrateTree(to, destTree, convertationRoot, convertationChild, Expression.ArrayIndex(path, Expression.Constant((int)edge.Value)), zzz);
                }
                else if(edge.Value is object[])
                {
                    if(convertationChild == null)
                    {
                        convertationChild = GoTo(convertationNode, ModelConfigurationEdge.Each);
                        if(convertationChild != null)
                            convertationChild = convertationChild.GotoMember(convertationChild.NodeType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance), false);
                    }
                    var indexes = (object[])edge.Value;
                    var method = path.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                    var parameters = method.GetParameters();
                    child.MigrateTree(to, destTree, convertationRoot, convertationChild, Expression.Call(path, method, indexes.Select((o, i) => Expression.Constant(o, parameters[i].ParameterType))), zzz);
                }
                else if(edge.Value is PropertyInfo || edge.Value is FieldInfo)
                    child.MigrateTree(to, destTree, convertationRoot, convertationChild, Expression.MakeMemberAccess(path, (MemberInfo)edge.Value), zzz);
                else if(edge.Value is Type)
                    child.MigrateTree(to, destTree, convertationRoot, convertationChild, Expression.Convert(path, (Type)edge.Value), zzz);
                else if(ReferenceEquals(edge.Value, MutatorsHelperFunctions.EachMethod))
                {
                    if(convertationChild != null || zzz)
                        child.MigrateTree(to, destTree, convertationRoot, convertationChild, Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(child.NodeType), path), zzz);
                    else if(convertationNode != null)
                    {
                        foreach(var dictionaryEntry in convertationNode.children)
                        {
                            var configurationEdge = dictionaryEntry.Key;
                            if(configurationEdge.Value is int)
                            {
                                var index = (int)configurationEdge.Value;
                                child.MigrateTree(to, destTree, convertationRoot, dictionaryEntry.Value, Expression.ArrayIndex(path, Expression.Constant(index)), zzz);
                            }
                            else if(configurationEdge.Value is object[])
                            {
                                var indexes = (object[])configurationEdge.Value;
                                var method = path.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
                                var parameters = method.GetParameters();
                                child.MigrateTree(to, destTree, convertationRoot, dictionaryEntry.Value, Expression.Call(path, method, indexes.Select((o, i) => Expression.Constant(o, parameters[i].ParameterType))), zzz);
                            }
                        }
                    }
                }
                else
                    throw new InvalidOperationException();
            }
        }

        private static void MigrateNode(this ModelConfigurationNode node, Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationRoot, Expression path)
        {
            var performer = new CompositionPerformer(node.RootType, to, convertationRoot);
            var parameters = new List<PathPrefix> {new PathPrefix(path, path.ExtractParameters().Single())};
            var abstractPathResolver = new AbstractPathResolver(parameters, false);

            foreach(var mutator in node.mutators)
            {
                var mutatedMutator = mutator.Value.Mutate(to, path, performer);
                if(mutatedMutator == null)
                    continue;
                var resolvedKey = abstractPathResolver.Resolve(mutator.Key);
                var conditionalSetters = performer.GetConditionalSetters(resolvedKey);
                if(conditionalSetters != null)
                    Qxx(destTree, conditionalSetters, mutatedMutator, performer, resolvedKey);
                else
                {
                    Expression mutatedPath = Expression.Constant(null);
                    if(resolvedKey.NodeType == ExpressionType.NewArrayInit && resolvedKey.Type == typeof(object[]))
                    {
                        var paths = new List<Expression>();
                        // Mutator is set to a number of nodes
                        foreach(var item in ((NewArrayExpression)resolvedKey).Expressions)
                        {
                            var mutatedItem = performer.Perform(item);
                            if(mutatedItem.NodeType != ExpressionType.Constant || ((ConstantExpression)mutatedItem).Value != null)
                                paths.Add(mutatedItem);
                            else
                            {
                                // Mutator is set to a node that is either not a leaf or a leaf that is not convertible
                                var primaryDependencies = Expression.Lambda(item, item.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body).ToArray();
                                if(primaryDependencies.Length > 1)
                                    throw new NotSupportedException("More than one primary dependency is not supported while migrating a mutator from a non-leaf node");
                                var subRoot = convertationRoot.Traverse(primaryDependencies[0], false);
                                if(subRoot != null)
                                {
                                    var keyLeaf = subRoot.FindKeyLeaf();
                                    if(keyLeaf != null)
                                    {
                                        var keyLeafPath = abstractPathResolver.Resolve(keyLeaf.Path);
                                        conditionalSetters = performer.GetConditionalSetters(keyLeafPath);
                                        if(conditionalSetters != null)
                                        {
                                            paths.Add(performer.Perform(keyLeafPath));
                                            continue;
                                        }
                                    }
                                    // The key leaf is missing or is not convertible - list all convertible subnodes
                                    var subNodes = new List<ModelConfigurationNode>();
                                    subRoot.FindSubNodes(subNodes);
                                    paths.AddRange(subNodes.Select(x => performer.Perform(abstractPathResolver.Resolve(x.Path))));
                                }
                            }
                        }
                        if(paths.Count > 0)
                            mutatedPath = Expression.NewArrayInit(typeof(object), paths.Select(exp => Expression.Convert(exp, typeof(object))));
                    }
                    else
                    {
                        mutatedPath = performer.Perform(resolvedKey);
                        if(mutatedPath.NodeType == ExpressionType.Constant && ((ConstantExpression)mutatedPath).Value == null)
                        {
                            // Mutator is set to a node that is either not a leaf or a leaf that is not convertible
                            var primaryDependencies = Expression.Lambda(resolvedKey, resolvedKey.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body).ToArray();
                            if(primaryDependencies.Length > 1)
                                throw new NotSupportedException("More than one primary dependency is not supported while migrating a mutator from a non-leaf node");
                            if(primaryDependencies.Length > 0)
                            {
                                var subRoot = convertationRoot.Traverse(primaryDependencies[0], false);
                                if(subRoot != null)
                                {
                                    var keyLeaf = subRoot.FindKeyLeaf();
                                    if(keyLeaf != null)
                                        conditionalSetters = performer.GetConditionalSetters(abstractPathResolver.Resolve(keyLeaf.Path));
                                    if(conditionalSetters != null)
                                    {
                                        Qxx(destTree, conditionalSetters, mutatedMutator, performer, resolvedKey);
                                        continue;
                                    }
                                    // The key leaf is missing or is not convertible - list all convertible subnodes
                                    var subNodes = new List<ModelConfigurationNode>();
                                    subRoot.FindSubNodes(subNodes);
                                    mutatedPath = Expression.NewArrayInit(typeof(object), subNodes.Select(x => Expression.Convert(performer.Perform(abstractPathResolver.Resolve(x.Path)), typeof(object))));
                                }
                            }
                        }
                    }
                    var commonPath = Expression.Lambda(mutatedPath, mutatedPath.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body).FindLCP();
                    var destNode = commonPath == null ? destTree : destTree.Traverse(commonPath, true);
                    destNode.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(mutatedPath, mutatedMutator));
                }
            }
        }

        private static void Qxx(ModelConfigurationNode destTree, List<KeyValuePair<Expression, Expression>> conditionalSetters, MutatorConfiguration mutatedMutator, CompositionPerformer performer, Expression resolvedKey)
        {
            var unconditionalSetter = conditionalSetters.SingleOrDefault(pair => pair.Value == null);
            Expression invertedCondition = null;
            foreach(var setter in conditionalSetters)
            {
                var mutatedPath = setter.Key;
                var condition = setter.Value;
                if(condition == null)
                    continue;
                Expression currentInvertedCondition = Expression.Not(condition);
                invertedCondition = invertedCondition == null ? currentInvertedCondition : Expression.AndAlso(invertedCondition, currentInvertedCondition);
                var primaryDependencies = Expression.Lambda(mutatedPath, mutatedPath.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body);
                var commonPath = primaryDependencies.FindLCP();
                var destNode = commonPath == null ? destTree : destTree.Traverse(commonPath, true);
                destNode.mutators.Add(PurgeFilters(mutatedPath, mutatedMutator.If(Expression.Lambda(condition, condition.ExtractParameters()))));
            }
            {
                var mutatedPath = unconditionalSetter.Key ?? performer.Perform(resolvedKey);
                var primaryDependencies = Expression.Lambda(mutatedPath, mutatedPath.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body);
                var commonPath = primaryDependencies.FindLCP();
                var destNode = commonPath == null ? destTree : destTree.Traverse(commonPath, true);
                destNode.mutators.Add(PurgeFilters(mutatedPath, invertedCondition == null ? mutatedMutator : mutatedMutator.If(Expression.Lambda(invertedCondition, invertedCondition.ExtractParameters()))));
            }
        }

        private static KeyValuePair<Expression, MutatorConfiguration> PurgeFilters(Expression path, MutatorConfiguration mutator)
        {
            var filters = new List<LambdaExpression>();
            var cleanedPath = path.CleanFilters(filters);
            if(filters.Any(filter => filter != null))
            {
                var shards = path.SmashToSmithereens();
                var cleanedShards = cleanedPath.SmashToSmithereens();
                var aliases = new List<KeyValuePair<Expression, Expression>>();
                var i = 0;
                var j = 0;
                LambdaExpression condition = null;
                foreach(var filter in filters)
                {
                    while(!(shards[i].NodeType == ExpressionType.Call && (((MethodCallExpression)shards[i]).Method.IsCurrentMethod() || ((MethodCallExpression)shards[i]).Method.IsEachMethod())))
                        ++i;
                    while(!(cleanedShards[j].NodeType == ExpressionType.Call && (((MethodCallExpression)cleanedShards[j]).Method.IsCurrentMethod() || ((MethodCallExpression)cleanedShards[j]).Method.IsEachMethod())))
                        ++j;
                    if(filter == null)
                        continue;
                    aliases.Add(new KeyValuePair<Expression, Expression>(cleanedShards[j], shards[i]));
                    condition = condition == null ? filter : condition.AndAlso(filter, false);
                    ++i;
                    ++j;
                }
                mutator = mutator.ResolveAliases(new AliasesResolver(aliases)).If(condition);
            }
            return new KeyValuePair<Expression, MutatorConfiguration>(cleanedPath, mutator);
        }
    }
}