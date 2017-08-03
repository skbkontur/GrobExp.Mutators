using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using GrEmit.Utils;

using GrobExp.Mutators.Aggregators;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Exceptions;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    public class ModelConfigurationNode
    {
        private ModelConfigurationNode(Type rootType, Type nodeType, ModelConfigurationNode root, ModelConfigurationNode parent, ModelConfigurationEdge edge, Expression path)
        {
            RootType = rootType;
            NodeType = nodeType;
            Root = root ?? this;
            Parent = parent;
            Edge = edge;
            Path = path;
            mutators = new List<KeyValuePair<Expression, MutatorConfiguration>>();
        }

        public static ModelConfigurationNode CreateRoot(Type type)
        {
            return new ModelConfigurationNode(type, type, null, null, null, Expression.Parameter(type, type.Name));
        }

        public ModelConfigurationNode Traverse(Expression path, bool create)
        {
            List<KeyValuePair<Expression, Expression>> arrayAliases;
            return Traverse(path, create, out arrayAliases);
        }

        public ModelConfigurationNode Traverse(Expression path, bool create, out List<KeyValuePair<Expression, Expression>> arrayAliases)
        {
            ModelConfigurationNode result;
            arrayAliases = new List<KeyValuePair<Expression, Expression>>();
            Traverse(path, null, out result, create, arrayAliases);
            return result;
        }

        public void Migrate(Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationTree)
        {
            MigrateTree(to, destTree, convertationTree, convertationTree, Parent == null ? Path : Expression.Parameter(NodeType, NodeType.Name), false);
        }

        public LambdaExpression BuildStaticNodeValidator()
        {
            var parameter = Parent == null ? (ParameterExpression)Path : Expression.Parameter(NodeType, NodeType.Name);
            var result = Expression.Variable(typeof(List<ValidationResult>), "result");
            Expression initResult = Expression.Assign(result, Expression.New(listValidationResultConstructor));
            var validationResults = new List<Expression> {initResult};
            foreach(var mutator in mutators.Where(mutator => mutator.Value is ValidatorConfiguration))
            {
                var validator = (ValidatorConfiguration)mutator.Value;
                var ok = true;
                foreach(var dependency in validator.Dependencies ?? new LambdaExpression[0])
                {
                    ModelConfigurationNode child;
                    if(!Root.Traverse(dependency.Body, this, out child, false) || child != this)
                    {
                        ok = false;
                        break;
                    }
                }
                if(ok)
                {
                    var current = validator.Apply(new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, Path)});
                    if(current != null)
                        validationResults.Add(Expression.Call(result, listAddValidationResultMethod, current));
                }
            }
            validationResults.Add(result);
            Expression body = Expression.Block(new[] {result}, validationResults);
            return Expression.Lambda(body, parameter);
        }


        public ModelConfigurationNode GotoEachArrayElement(bool create)
        {
            return GetChild(ModelConfigurationEdge.Each, NodeType.GetItemType(), create);
        }

        public Expression GetAlienArray()
        {
            var arrays = GetArrays();
            Expression result;
            if(!arrays.TryGetValue(RootType, out result))
                return null;
            return ExpressionEquivalenceChecker.Equivalent(Expression.Lambda(result, result.ExtractParameters()).ExtractPrimaryDependencies()[0].Body, Path, false, true) ? null : result;
        }

        public Dictionary<Type, Expression> GetArrays()
        {
            return GetArrays(Path);
        }

        public void AddMutatorSmart(LambdaExpression path, MutatorConfiguration mutator)
        {
            path = (LambdaExpression)path.Simplify();
            LambdaExpression filter;
            var simplifiedPath = PathSimplifier.SimplifyPath(path, out filter);
            mutator = mutator.ResolveAliases(ExpressionAliaser.CreateAliasesResolver(simplifiedPath.Body, path.Body));
            Traverse(simplifiedPath.Body, true).AddMutator(path.Body, filter == null ? mutator : mutator.If(filter));
        }

        public void AddMutator(MutatorConfiguration mutator)
        {
            if(mutator.IsUncoditionalSetter())
            {
                for(var i = 0; i < mutators.Count; ++i)
                {
                    if(mutators[i].Value.IsUncoditionalSetter() && ExpressionEquivalenceChecker.Equivalent(Path, mutators[i].Key, false, false))
                    {
                        mutators[i] = new KeyValuePair<Expression, MutatorConfiguration>(Path, mutator);
                        return;
                    }
                }
            }
            mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(Path, mutator));
        }

        public void AddMutator(Expression path, MutatorConfiguration mutator)
        {
            if(mutator.IsUncoditionalSetter())
            {
                for(var i = 0; i < mutators.Count; ++i)
                {
                    if(mutators[i].Value.IsUncoditionalSetter() && ExpressionEquivalenceChecker.Equivalent(path, mutators[i].Key, false, false))
                    {
                        mutators[i] = new KeyValuePair<Expression, MutatorConfiguration>(path, mutator);
                        return;
                    }
                }
            }
            mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(path, mutator));
        }

        public override string ToString()
        {
            return this.ToPrettyString();
        }

        public MutatorConfiguration[] GetMutators()
        {
            return mutators.Select(pair => pair.Value).ToArray();
        }

        public MutatorWithPath[] GetMutatorsWithPath()
        {
            return mutators.Select(mutator => new MutatorWithPath
                {
                    PathToNode = Path,
                    PathToMutator = mutator.Key,
                    Mutator = mutator.Value
                }).ToArray();
        }

        public void ExtractValidationsFromConverters(ModelConfigurationNode validationsTree)
        {
            var performer = new CompositionPerformer(RootType, validationsTree.RootType, this);
            ExtractValidationsFromConvertersInternal(validationsTree, performer);
        }

        public void FindSubNodes(List<ModelConfigurationNode> result)
        {
            if(children.Count == 0 && mutators.Count > 0)
                result.Add(this);
            foreach(var child in Children)
                child.FindSubNodes(result);
        }

        public ModelConfigurationNode FindKeyLeaf()
        {
            foreach(var entry in children)
            {
                var edge = entry.Key;
                var child = entry.Value;
                var property = edge.Value as PropertyInfo;
                if(property != null && property.GetCustomAttributes(typeof(KeyLeafAttribute), false).Any() && child.children.Count == 0)
                    return child;
                var result = child.FindKeyLeaf();
                if(result != null)
                    return result;
            }
            return null;
        }

        public ModelConfigurationNode GotoConvertation(Type type, bool create)
        {
            var edge = new ModelConfigurationEdge(type);
            return GetChild(edge, type, create);
        }

        public ModelConfigurationNode GotoMember(MemberInfo member, bool create)
        {
            var edge = new ModelConfigurationEdge(member);
            switch(member.MemberType)
            {
            case MemberTypes.Field:
                return GetChild(edge, ((FieldInfo)member).FieldType, create);
            case MemberTypes.Property:
                return GetChild(edge, ((PropertyInfo)member).PropertyType, create);
            default:
                throw new NotSupportedException("Member rootType " + member.MemberType + " is not supported");
            }
        }

        public Expression Path { get; private set; }
        public IEnumerable<ModelConfigurationNode> Children { get { return children.Values.Cast<ModelConfigurationNode>(); } }
        public Type NodeType { get; private set; }
        public Type RootType { get; set; }
        
        internal bool Traverse(Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create)
        {
            return Traverse(path, subRoot, out child, create, new List<KeyValuePair<Expression, Expression>>());
        }

        /// <summary>
        ///     Traverses the <paramref name="path" /> from this node, creating missing nodes if <paramref name="create" /> is true. <br />
        ///     At the end, <paramref name="child" /> points to the last traversed node and <paramref name="arrayAliases" /> contains some shit.
        /// </summary>
        /// <returns>
        ///     True, if <paramref name="subRoot" /> node was visited while traversing from root node by <paramref name="path" />
        /// </returns>
        internal bool Traverse(Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create, List<KeyValuePair<Expression, Expression>> arrayAliases)
        {
            switch(path.NodeType)
            {
            case ExpressionType.Parameter:
                {
                    //Check if we have reached root of the tree
                    if(path.Type != NodeType)
                    {
                        // Shit happened
                        child = null;
                        return false;
                    }
                    // Ok, it's root
                    child = this;
                    return subRoot == this;
                }
            case ExpressionType.Convert:
                {
                    // Traverse to the operand's node and go by 'convertation' edge.
                    var unaryExpression = (UnaryExpression)path;
                    var result = Traverse(unaryExpression.Operand, subRoot, out child, create, arrayAliases);
                    child = child == null ? null : child.GotoConvertation(unaryExpression.Type, create);
                    return result || child == subRoot;
                }
            case ExpressionType.MemberAccess:
                {
                    // Traverse to the node of containing object and go by 'member-access' edge
                    var memberExpression = (MemberExpression)path;
                    var result = Traverse(memberExpression.Expression, subRoot, out child, create, arrayAliases);
                    child = child == null ? null : child.GotoMember(memberExpression.Member, create);
                    return result || child == subRoot;
                }
            case ExpressionType.ArrayIndex:
                {
                    var binaryExpression = (BinaryExpression)path;
                    // Traverse to the array's node
                    var result = Traverse(binaryExpression.Left, subRoot, out child, create, arrayAliases);
                    if(child != null)
                    {
                        // Try go by 'array-index' edge
                        var newChild = child.GotoArrayElement(GetIndex(binaryExpression.Right), create);
                        // If no child exists and create:false go by 'each' edge and create array alias for migration
                        if(newChild == null)
                        {
                            newChild = child.GotoEachArrayElement(create : false);
                            var array = GetArrayMigrationAlias(child);
                            if(array != null)
                            {
                                arrayAliases.Add(new KeyValuePair<Expression, Expression>(
                                                     Expression.ArrayIndex(array, binaryExpression.Right),
                                                     array.MakeEachCall())
                                    );
                            }
                        }
                        child = newChild;
                    }
                    return result || child == subRoot;
                }
            case ExpressionType.Call:
                {
                    var methodCallExpression = (MethodCallExpression)path;
                    var method = methodCallExpression.Method;
                    if(method.IsEachMethod() || method.IsCurrentMethod())
                    {
                        // Traverse to the array's node which is in Arguments[0], because Each() and Current() are extension methods, and go by 'each' edge
                        var result = Traverse(methodCallExpression.Arguments[0], subRoot, out child, create, arrayAliases);
                        child = child == null ? null : child.GotoEachArrayElement(create);
                        return result || child == subRoot;
                    }
                    if(method.IsArrayIndexer())
                    {
                        // Traverse to the array's node which is Object of method call
                        var result = Traverse(methodCallExpression.Object, subRoot, out child, create, arrayAliases);
                        if(child != null)
                        {
                            // Try go by 'array-index' edge
                            var newChild = child.GotoArrayElement(GetIndex(methodCallExpression.Arguments[0]), create);
                            // If no child exists and create:false go by 'each' edge and create array alias for migration
                            if(newChild == null)
                            {
                                newChild = child.GotoEachArrayElement(false);
                                var array = GetArrayMigrationAlias(child);
                                if(array != null)
                                {
                                    arrayAliases.Add(new KeyValuePair<Expression, Expression>(
                                                         Expression.ArrayIndex(array, methodCallExpression.Arguments[0]),
                                                         array.MakeEachCall())
                                        );
                                }
                            }
                            child = newChild;
                        }
                        return result || child == subRoot;
                    }
                    if(method.IsIndexerGetter())
                    {
                        // Traverse to the object's node which is Object of method call
                        var result = Traverse(methodCallExpression.Object, subRoot, out child, create, arrayAliases);
                        if(child != null)
                        {
                            var parameters = methodCallExpression.Arguments.Select(exp => ((ConstantExpression)exp).Value).ToArray();
                            // Try go by 'indexer' edge
                            var newChild = child.GotoIndexer(parameters, create);
                            // If no child exists and create:false go by 'each' edge and create array alias for migration
                            if(newChild == null)
                            {
                                newChild = child.GotoEachArrayElement(false);
                                if(newChild != null)
                                {
                                    newChild = newChild.GotoMember(newChild.NodeType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance), false);
                                    var array = GetArrayMigrationAlias(child);
                                    if(array != null)
                                    {
                                        arrayAliases.Add(new KeyValuePair<Expression, Expression>(
                                                             Expression.Call(array, array.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), methodCallExpression.Arguments),
                                                             Expression.Property(array.MakeEachCall(), "Value"))
                                            );
                                    }
                                }
                            }
                            child = newChild;
                        }
                        return result || child == subRoot;
                    }
                    throw new NotSupportedException("Method " + method + " is not supported");
                }
            case ExpressionType.ArrayLength:
                {
                    // Traverse to the array's node and go by 'length' edge

//                    if(create)
//                        throw new NotSupportedException("Node type " + path.NodeType + " is not supported");
                    var unaryExpression = (UnaryExpression)path;
                    var result = Traverse(unaryExpression.Operand, subRoot, out child, create, arrayAliases);
                    child = child == null ? null : child.GotoMember(ModelConfigurationEdge.ArrayLengthProperty, create);
                    return result || child == subRoot;
                }
            default:
                throw new NotSupportedException("Node type " + path.NodeType + " is not supported");
            }
        }

        private Expression GetArrayMigrationAlias(ModelConfigurationNode node)
        {
            var arrays = node.GetArrays();
            var childPath = new ExpressionWrapper(node.Path, strictly : false);
            return arrays.Values.FirstOrDefault(arrayPath => !childPath.Equals(new ExpressionWrapper(arrayPath, strictly : false)));
        }

        private static int GetIndex(Expression exp)
        {
            // todo использовать ExpressionCompiler
            if(exp.NodeType == ExpressionType.Constant)
                return (int)((ConstantExpression)exp).Value;
            return Expression.Lambda<Func<int>>(Expression.Convert(exp, typeof(int))).Compile()();
        }

        private ModelConfigurationNode GotoIndexer(object[] parameters, bool create)
        {
            var edge = new ModelConfigurationEdge(parameters);
            var property = NodeType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            if(property == null)
                throw new InvalidOperationException("Type '" + NodeType + "' doesn't contain indexer");
            if(!property.CanRead || !property.CanWrite)
                throw new InvalidOperationException("Type '" + NodeType + "' has indexer that doesn't contain either getter or setter");
            return GetChild(edge, property.GetGetMethod().ReturnType, create);
        }

        private ModelConfigurationNode GotoArrayElement(int index, bool create)
        {
            return GetChild(new ModelConfigurationEdge(index), NodeType.GetItemType(), create);
        }

        private void ExtractValidationsFromConvertersInternal(ModelConfigurationNode validationsTree, CompositionPerformer performer)
        {
            foreach(var mutator in mutators)
            {
                var equalsToConfiguration = mutator.Value as EqualsToConfiguration;
                if(equalsToConfiguration != null && equalsToConfiguration.Validator != null)
                {
                    var path = equalsToConfiguration.Validator.PathToNode;
                    var primaryDependencies = path.ExtractPrimaryDependencies().Select(lambda => lambda.Body);
                    var commonPath = primaryDependencies.FindLCP();
                    var node = commonPath == null ? validationsTree : validationsTree.Traverse(commonPath, true);
                    var mutatedValidator = equalsToConfiguration.Validator.Mutate(RootType, commonPath, performer);
                    if(mutatedValidator != null)
                        node.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(equalsToConfiguration.Validator.PathToValue.Body, mutatedValidator));
                }
            }
            foreach(var child in Children)
                child.ExtractValidationsFromConvertersInternal(validationsTree, performer);
        }

        private static ModelConfigurationNode GoTo(ModelConfigurationNode node, ModelConfigurationEdge edge)
        {
            if(node == null)
                return null;
            ModelConfigurationNode result;
            return node.children.TryGetValue(edge, out result) ? result : null;
        }

        private void MigrateTree(Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationRoot, ModelConfigurationNode convertationNode, Expression path, bool zzz)
        {
            MigrateNode(to, destTree, convertationRoot, path);
            if(!zzz && convertationNode != null && convertationNode.mutators.Any(pair => pair.Value is EqualsToConfiguration))
                zzz = true;
            foreach(var entry in children)
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

        private void MigrateNode(Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationRoot, Expression path)
        {
            var performer = new CompositionPerformer(RootType, to, convertationRoot);
            var parameters = new List<PathPrefix> {new PathPrefix(path, path.ExtractParameters().Single())};
            var abstractPathResolver = new AbstractPathResolver(parameters, false);

            foreach(var mutator in mutators)
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
                                    paths.AddRange(subNodes.Select(node => performer.Perform(abstractPathResolver.Resolve(node.Path))));
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
                                    mutatedPath = Expression.NewArrayInit(typeof(object), subNodes.Select(node => Expression.Convert(performer.Perform(abstractPathResolver.Resolve(node.Path)), typeof(object))));
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



        /// <summary>
        ///     Runs <see cref="ArraysExtractor" /> for all mutators in all nodes in the sub-tree and
        ///     saves to <paramref name="arrays" /> all expressions with level (count of Each() and Current() calls)
        ///     matching the level of <paramref name="path" />.
        /// </summary>
        private void GetArrays(Expression path, Dictionary<Type, List<Expression>> arrays)
        {
            if(mutators != null && mutators.Count > 0)
            {
                var shards = path.SmashToSmithereens();
                var level = 1;
                foreach(var shard in shards)
                {
                    if(shard.NodeType == ExpressionType.Call && ((MethodCallExpression)shard).Method.IsEachMethod())
                        ++level;
                }

                var list = new List<Dictionary<Type, List<Expression>>>();
                var arraysExtractor = new ArraysExtractor(list);

                foreach(var mutator in mutators)
                    mutator.Value.GetArrays(arraysExtractor);

                if(list.Count > level)
                {
                    var dict = list[level];
                    foreach(var pair in dict)
                    {
                        List<Expression> lizd;
                        if(!arrays.TryGetValue(pair.Key, out lizd))
                            arrays.Add(pair.Key, lizd = new List<Expression>());
                        lizd.AddRange(pair.Value);
                    }
                }
            }
            foreach(var child in children.Values)
                child.GetArrays(path, arrays);
        }

        private Dictionary<Type, Expression> GetArrays(Expression path)
        {
            var arrays = new Dictionary<Type, List<Expression>>();
            GetArrays(path, arrays);
            return arrays
                .Where(pair => pair.Value.Count > 0)
                .ToDictionary(pair => pair.Key,
                              pair => pair.Value
                                          .GroupBy(expression => new ExpressionWrapper(expression, strictly : false))
                                          .Select(grouping =>
                                              {
                                                  var exp = grouping.First();
                                                  return exp.NodeType == ExpressionType.Call ? ((MethodCallExpression)exp).Arguments[0] : exp;
                                              })
                                          .FirstOrDefault());
        }

        private ModelConfigurationNode GetChild(ModelConfigurationEdge edge, Type childType, bool create)
        {
            ModelConfigurationNode child;
            if(!children.TryGetValue(edge, out child) && create)
            {
                Expression path;
                if(edge.IsArrayIndex)
                    path = Path.MakeArrayIndex((int)edge.Value);
                else if(edge.IsMemberAccess)
                    path = Path.MakeMemberAccess((MemberInfo)edge.Value);
                else if(edge.IsEachMethod)
                    path = Path.MakeEachCall(childType);
                else if(edge.IsConvertation)
                    path = Path.MakeConvertation((Type)edge.Value);
                else if(edge.IsIndexerParams)
                    path = Path.MakeIndexerCall((object[])edge.Value, NodeType);
                else throw new InvalidOperationException();

                child = new ModelConfigurationNode(RootType, childType, Root, this, edge, path);
                children.Add(edge, child);
            }
            return child;
        }

        internal ModelConfigurationNode Root { get; private set; }
        internal ModelConfigurationNode Parent { get; private set; }
        internal ModelConfigurationEdge Edge { get; private set; }
        internal readonly List<KeyValuePair<Expression, MutatorConfiguration>> mutators;

        private static readonly MethodInfo listAddValidationResultMethod = ((MethodCallExpression)((Expression<Action<List<ValidationResult>>>)(list => list.Add(null))).Body).Method;
        private static readonly ConstructorInfo listValidationResultConstructor = ((NewExpression)((Expression<Func<List<ValidationResult>>>)(() => new List<ValidationResult>())).Body).Constructor;
        

        internal readonly Dictionary<ModelConfigurationEdge, ModelConfigurationNode> children = new Dictionary<ModelConfigurationEdge, ModelConfigurationNode>();

        
    }
}