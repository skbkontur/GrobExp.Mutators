using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Exceptions;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
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
            PathText = ExtractText(path);
            mutators = new List<KeyValuePair<Expression, MutatorConfiguration>>();
        }

        public static ModelConfigurationNode CreateRoot(Type type)
        {
            return new ModelConfigurationNode(type, type, null, null, null, Expression.Parameter(type));
        }

        public ModelConfigurationNode Traverse(Expression path, bool create)
        {
            ModelConfigurationNode result;
            Traverse(path, null, out result, create);
            return result;
        }

        public void Migrate(Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationTree)
        {
            MigrateTree(to, destTree, convertationTree, convertationTree, Parent == null ? Path : Expression.Parameter(NodeType), false);
        }

        public LambdaExpression BuildTreeValidator(IPathFormatter pathFormatter)
        {
            var mutators = new Dictionary<ExpressionWrapper, List<MutatorConfiguration>>();
            GetMutators(mutators);

            var root = new ZzzNode();
            foreach(var pair in mutators)
            {
                var arrays = GetArrays(RootType, pair.Key.Expression, pair.Value);
                var node = arrays.Aggregate(root, (current, array) => current.Traverse(array));
                node.mutators.Add(new KeyValuePair<Expression, List<MutatorConfiguration>>(pair.Key.Expression, pair.Value));
            }

            var parameter = Parent == null ? (ParameterExpression)Path : Expression.Parameter(NodeType);
            var result = Expression.Parameter(typeof(ValidationResultTreeNode), "tree");
            var priority = Expression.Parameter(typeof(int), "priority");
            var aliases = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, Path)};

            root = GetArrays(RootType, Path, new MutatorConfiguration[0]).Aggregate(root, (current, array) => current.Traverse(array));

            var validationResults = new List<Expression>();
            root.BuildValidator(pathFormatter, this, aliases, result, priority, validationResults);

            Expression body;
            switch(validationResults.Count)
            {
            case 0:
                body = Expression.Empty();
                break;
            case 1:
                body = validationResults[0];
                break;
            default:
                body = Expression.Block(validationResults);
                break;
            }
            var lambda = Expression.Lambda(body.ExtendSelectMany(), parameter, result, priority);
            return lambda;
        }

        public LambdaExpression BuildStaticNodeValidator()
        {
            var parameter = Parent == null ? (ParameterExpression)Path : Expression.Parameter(NodeType);
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
                        validationResults.Add(Expression.Call(result, listAddValidationResultMethod, new[] {current}));
                }
            }
            validationResults.Add(result);
            Expression body = Expression.Block(new[] {result}, validationResults);
            return Expression.Lambda(body, parameter);
        }

        public LambdaExpression BuildTreeMutator()
        {
            return BuildTreeMutator(new List<ParameterExpression> {Parent == null ? (ParameterExpression)Path : Expression.Parameter(NodeType)});
        }

        public LambdaExpression BuildTreeMutator(Type type)
        {
            return BuildTreeMutator(new List<ParameterExpression> {Parent == null ? (ParameterExpression)Path : Expression.Parameter(NodeType), Expression.Parameter(type)});
        }

        public ModelConfigurationNode GotoEachArrayElement(bool create)
        {
            return GetChild(ModelConfigurationEdge.Each, NodeType.GetElementType(), create);
        }

        public Expression GetAlienArray()
        {
            var arrays = GetArrays(true);
            Expression result;
            if(!arrays.TryGetValue(RootType, out result))
                return null;
            return ExpressionEquivalenceChecker.Equivalent(result, Path, false) ? null : result;
        }

        public Dictionary<Type, Expression> GetArrays(bool cutTail)
        {
            return GetArrays(Path, cutTail);
        }

        public void AddMutator(MutatorConfiguration mutator)
        {
            mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(Path, mutator));
        }

        public MutatorConfiguration[] GetMutators()
        {
            return mutators.Select(pair => pair.Value).ToArray();
        }

        public void ExtractValidationsFromConverters(ModelConfigurationNode validationsTree)
        {
            var performer = new CompositionPerformer(RootType, validationsTree.RootType, this, null);
            ExtractValidationsFromConvertersInternal(validationsTree, performer);
        }

        public Expression Path { get; private set; }
        public string PathText { get; private set; }
        public IEnumerable<ModelConfigurationNode> Children { get { return children.Values.Cast<ModelConfigurationNode>(); } }
        public Type NodeType { get; private set; }
        public Type RootType { get; set; }

        private void GetMutators(Dictionary<ExpressionWrapper, List<MutatorConfiguration>> result)
        {
            if(mutators != null)
            {
                foreach(var pair in mutators)
                {
                    var key = new ExpressionWrapper(pair.Key, false);
                    List<MutatorConfiguration> list;
                    if(!result.TryGetValue(key, out list))
                        result.Add(key, list = new List<MutatorConfiguration>());
                    list.Add(pair.Value);
                }
            }
            foreach(var child in Children)
                child.GetMutators(result);
        }

        private static IEnumerable<Expression> GetArrays(Type rootType, Expression path, IEnumerable<MutatorConfiguration> mutators)
        {
            var arrays = new List<Dictionary<Type, List<Expression>>>();
            var arraysExtractor = new ArraysExtractor(arrays);
            arraysExtractor.GetArrays(path);
            foreach(var mutator in mutators)
                mutator.GetArrays(arraysExtractor);
            var result = new List<Expression>();
            var replacer = new MethodReplacer(MutatorsHelperFunctions.CurrentMethod, MutatorsHelperFunctions.EachMethod);
            for(var i = 1; i < arrays.Count; ++i)
            {
                var dict = arrays[i];
                if(dict.Count > 1)
                    throw new InvalidOperationException("Too many root types");
                List<Expression> list;
                if(!dict.TryGetValue(rootType, out list))
                    throw new InvalidOperationException("Invalid root type");
                var arraysOfCurrentLevel = list.GroupBy(exp => new ExpressionWrapper(replacer.Visit(exp), false)).Select(grouping => grouping.First()).ToArray();
                if(arraysOfCurrentLevel.Length > 1)
                    throw new NotSupportedException("Iteration over more than one array is not supported");
                result.Add(arraysOfCurrentLevel[0]);
            }
            return result;
        }

        private static string ExtractText(Expression path)
        {
            var result = new StringBuilder("root");
            var shards = path.SmashToSmithereens();
            for(var i = 1; i < shards.Length; ++i)
            {
                result.Append('.');
                var shard = shards[i];
                switch(shard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    result.Append(((MemberExpression)shard).Member.Name);
                    break;
                case ExpressionType.ArrayIndex:
                    result.Append(((ConstantExpression)((BinaryExpression)shard).Right).Value);
                    break;
                case ExpressionType.Call:
                    result.Append("Each()");
                    break;
                default:
                    throw new InvalidOperationException("Node type '" + shard.NodeType + "' is not valid at this point");
                }
            }
            return result.ToString();
        }

        private bool Traverse(Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create)
        {
            switch(path.NodeType)
            {
            case ExpressionType.Parameter:
                {
                    if(path.Type != NodeType)
                    {
                        child = null;
                        return false;
                    }
                    child = this;
                    return subRoot == this;
                }
            case ExpressionType.MemberAccess:
                {
                    var memberExpression = (MemberExpression)path;
                    var result = Traverse(memberExpression.Expression, subRoot, out child, create);
                    child = child == null ? null : child.GotoMember(memberExpression.Member, create);
                    return result || child == subRoot;
                }
            case ExpressionType.ArrayIndex:
                {
                    var binaryExpression = (BinaryExpression)path;
                    var result = Traverse(binaryExpression.Left, subRoot, out child, create);
                    child = child == null ? null : (child.GotoArrayElement(GetIndex(binaryExpression.Right), create) ?? child.GotoEachArrayElement(false));
                    return result || child == subRoot;
                }
            case ExpressionType.Call:
                {
                    var methodCallExpression = (MethodCallExpression)path;
                    if(!(methodCallExpression.Method.IsEachMethod() || methodCallExpression.Method.IsCurrentMethod()))
                        throw new NotSupportedException("Method " + methodCallExpression.Method + " is not supported");
                    var result = Traverse(methodCallExpression.Arguments[0], subRoot, out child, create);
                    child = child == null ? null : child.GotoEachArrayElement(create);
                    return result || child == subRoot;
                }
            case ExpressionType.Convert:
                return Traverse(((UnaryExpression)path).Operand, subRoot, out child, create);
            case ExpressionType.ArrayLength:
                {
                    if(create)
                        throw new NotSupportedException("Node type " + path.NodeType + " is not supported");
                    var unaryExpression = (UnaryExpression)path;
                    var result = Traverse(unaryExpression.Operand, subRoot, out child, false);
                    return result || child == subRoot;
                }
            default:
                throw new NotSupportedException("Node type " + path.NodeType + " is not supported");
            }
        }

        private static int GetIndex(Expression exp)
        {
            // todo использовать ExpressionCompiler
            if(exp.NodeType == ExpressionType.Constant)
                return (int)((ConstantExpression)exp).Value;
            return Expression.Lambda<Func<int>>(Expression.Convert(exp, typeof(int))).Compile()();
        }

        private ModelConfigurationNode GotoMember(MemberInfo member, bool create)
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

        private ModelConfigurationNode GotoArrayElement(int index, bool create)
        {
            return GetChild(new ModelConfigurationEdge(index), NodeType.GetElementType(), create);
        }

        private void ExtractValidationsFromConvertersInternal(ModelConfigurationNode validationsTree, CompositionPerformer performer)
        {
            foreach(var mutator in mutators)
            {
                var equalsToConfiguration = mutator.Value as EqualsToConfiguration;
                if(equalsToConfiguration != null && equalsToConfiguration.Validator != null)
                {
                    var path = equalsToConfiguration.Validator.Path;
                    var primaryDependencies = path.ExtractPrimaryDependencies().Select(lambda => lambda.Body);
                    var commonPath = primaryDependencies.FindLCP();
                    var node = commonPath == null ? validationsTree : validationsTree.Traverse(commonPath, true);
                    node.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(path.Body, equalsToConfiguration.Validator.Mutate(RootType, commonPath, performer)));
                }
            }
            foreach(var child in Children)
                child.ExtractValidationsFromConvertersInternal(validationsTree, performer);
        }

        private void MigrateTree(Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationRoot, ModelConfigurationNode convertationNode, Expression path, bool mapsSomewhereAbove)
        {
            mapsSomewhereAbove |= MigrateNode(to, destTree, convertationRoot, path);
            foreach(DictionaryEntry entry in children)
            {
                var edge = (ModelConfigurationEdge)entry.Key;
                var child = (ModelConfigurationNode)entry.Value;
                var convertationChild = convertationNode == null ? null : (ModelConfigurationNode)convertationNode.children[edge];
                if(edge.Value is int)
                {
                    if(convertationChild == null)
                        convertationChild = convertationNode == null ? null : (ModelConfigurationNode)convertationNode.children[ModelConfigurationEdge.Each];
                    if(mapsSomewhereAbove || convertationChild != null)
                        child.MigrateTree(to, destTree, convertationRoot, convertationChild, Expression.ArrayIndex(path, Expression.Constant((int)edge.Value)), mapsSomewhereAbove);
                }
                else if(edge.Value is PropertyInfo || edge.Value is FieldInfo)
                {
                    if(mapsSomewhereAbove || convertationChild != null)
                        child.MigrateTree(to, destTree, convertationRoot, convertationChild, Expression.MakeMemberAccess(path, (MemberInfo)edge.Value), mapsSomewhereAbove);
                }
                else if(ReferenceEquals(edge.Value, MutatorsHelperFunctions.EachMethod))
                {
                    if(mapsSomewhereAbove || convertationChild != null)
                        child.MigrateTree(to, destTree, convertationRoot, convertationChild, Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(child.NodeType), new[] {path}), mapsSomewhereAbove);
                    else if(convertationNode != null)
                    {
                        foreach(DictionaryEntry dictionaryEntry in convertationNode.children)
                        {
                            var configurationEdge = (ModelConfigurationEdge)dictionaryEntry.Key;
                            if(!(configurationEdge.Value is int)) continue;
                            var index = (int)configurationEdge.Value;
                            child.MigrateTree(to, destTree, convertationRoot, (ModelConfigurationNode)dictionaryEntry.Value, Expression.ArrayIndex(path, Expression.Constant(index)), false);
                        }
                    }
                }
                else
                    throw new InvalidOperationException();
            }
        }

        private bool MigrateNode(Type to, ModelConfigurationNode destTree, ModelConfigurationNode convertationRoot, Expression path)
        {
            var performer = new CompositionPerformer(RootType, to, convertationRoot, null);
            var parameters = new List<PathPrefix> {new PathPrefix(path, path.ExtractParameters().Single())};

            foreach(var mutator in mutators)
            {
                var mutatedMutator = mutator.Value.Mutate(to, path, performer);
                var resolvedKey = new AbstractPathResolver(parameters, false).Resolve(mutator.Key);
                var conditionalSetters = performer.GetConditionalSetters(resolvedKey);
                if(conditionalSetters == null)
                {
                    var mutatedPath = performer.Perform(resolvedKey);
                    if(mutatedPath == null)
                        throw new InvalidOperationException("Unable to migrate node '" + path + "'");
                    var primaryDependencies = Expression.Lambda(mutatedPath, mutatedPath.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body);
                    var commonPath = primaryDependencies.FindLCP();
                    var destNode = commonPath == null ? destTree : destTree.Traverse(commonPath, true);
                    destNode.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(mutatedPath, mutatedMutator));
                }
                else
                {
                    if(conditionalSetters.Count == 1)
                    {
                        var mutatedPath = conditionalSetters.Single().Key;
                        var primaryDependencies = Expression.Lambda(mutatedPath, mutatedPath.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body);
                        var commonPath = primaryDependencies.FindLCP();
                        var destNode = commonPath == null ? destTree : destTree.Traverse(commonPath, true);
                        destNode.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(mutatedPath, mutatedMutator));
                    }
                    else
                    {
                        foreach(var setter in conditionalSetters)
                        {
                            var mutatedPath = setter.Key;
                            var condition = setter.Value;
                            var primaryDependencies = Expression.Lambda(mutatedPath, mutatedPath.ExtractParameters()).ExtractPrimaryDependencies().Select(lambda => lambda.Body);
                            var commonPath = primaryDependencies.FindLCP();
                            var destNode = commonPath == null ? destTree : destTree.Traverse(commonPath, true);
                            destNode.mutators.Add(new KeyValuePair<Expression, MutatorConfiguration>(mutatedPath, mutatedMutator.If(Expression.Lambda(condition, condition.ExtractParameters()))));
                        }
                    }
                }
            }
            return performer.GetConditionalSetters(path) != null;
        }

        private LambdaExpression BuildTreeMutator(List<ParameterExpression> parameters)
        {
            var visitedNodes = new HashSet<ModelConfigurationNode>();
            var processedNodes = new HashSet<ModelConfigurationNode>();
            var mutators = new List<Expression>();
            var aliases = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameters[0], Path)};
            BuildTreeMutator(null, this, Path, parameters[0], aliases, mutators, visitedNodes, processedNodes, mutators);
            mutators.Add(Expression.Empty());
            Expression body = Expression.Block(mutators);
            foreach(var actualParameter in body.ExtractParameters())
            {
                var expectedParameter = parameters.Single(p => p.Type == actualParameter.Type);
                if(actualParameter != expectedParameter)
                    body = new ParameterReplacer(actualParameter, expectedParameter).Visit(body);
            }
            var result = Expression.Lambda(body.ExtendSelectMany(), parameters);
            return result;
        }

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
            foreach(ModelConfigurationNode child in children.Values)
                child.GetArrays(path, arrays);
        }

        private Dictionary<Type, Expression> GetArrays(Expression path, bool cutTail)
        {
            var arrays = new Dictionary<Type, List<Expression>>();
            GetArrays(path, arrays);
            return arrays.Where(pair => pair.Value.Count > 0).ToDictionary(pair => pair.Key, pair => pair.Value.GroupBy(expression => new ExpressionWrapper(expression, false)).Select(
                grouping =>
                    {
                        var exp = grouping.First();
                        return cutTail && exp.NodeType == ExpressionType.Call ? ((MethodCallExpression)exp).Arguments[0] : exp;
                    }).FirstOrDefault());
        }

        private static Expression CacheExternalExpressions(Expression expression, Func<Expression, Expression> resultSelector, params ParameterExpression[] internalParameters)
        {
            Expression result;
            var externalExpressions = new ExternalExpressionsExtractor(internalParameters).Extract(expression);
            if(externalExpressions.Length == 0)
                result = resultSelector(expression);
            else
            {
                var aliases = new List<KeyValuePair<Expression, Expression>>();
                var variables = new List<ParameterExpression>();
                foreach(var exp in externalExpressions)
                {
                    var variable = Expression.Variable(exp.Type);
                    variables.Add(variable);
                    aliases.Add(new KeyValuePair<Expression, Expression>(variable, exp));
                }
                var optimizedExpression = expression.ResolveAliases(aliases, true);
                result = Expression.Block(variables, aliases.Select(pair => Expression.Assign(pair.Key, pair.Value)).Concat(new[] {resultSelector(optimizedExpression)}));
            }
            return result;
        }

        private static void CheckDependencies(ModelConfigurationNode root, MutatorConfiguration mutator)
        {
            if(mutator == null || mutator.Dependencies == null)
                return;
            foreach(var dependency in mutator.Dependencies)
            {
                ModelConfigurationNode child;
                if(!root.Root.Traverse(dependency.Body, root, out child, false))
                    throw new FoundExternalDependencyException("Unable to build validator for the subtree '" + root.Parent + "' due to the external dependency '" + dependency + "'");
            }
        }

        private void BuildTreeMutator(ModelConfigurationEdge edge, Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, Expression fullPath, Expression path, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult,
                                      HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult)
        {
            var child = (ModelConfigurationNode)children[edge];
            if(edge.Value is PropertyInfo || edge.Value is FieldInfo)
                child.BuildTreeMutator(edges, root, Expression.MakeMemberAccess(fullPath, (MemberInfo)edge.Value), Expression.MakeMemberAccess(path, (MemberInfo)edge.Value), aliases, localResult, visitedNodes, processedNodes, globalResult);
            else if(edge.Value is int)
                child.BuildTreeMutator(edges, root, Expression.ArrayIndex(fullPath, Expression.Constant((int)edge.Value)), Expression.ArrayIndex(path, Expression.Constant((int)edge.Value)), aliases, localResult, visitedNodes, processedNodes, globalResult);
            else if(ReferenceEquals(edge.Value, MutatorsHelperFunctions.EachMethod))
            {
                var childParameter = Expression.Parameter(child.NodeType);
                var indexParameter = Expression.Parameter(typeof(int));
                var item = Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(child.NodeType), new[] {fullPath});
                var index = Expression.Call(null, MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(child.NodeType), new Expression[] {item});
                aliases.Add(new KeyValuePair<Expression, Expression>(childParameter, item));
                aliases.Add(new KeyValuePair<Expression, Expression>(indexParameter, index));
                // todo ich: почему только первый?
                var array = GetArrays(fullPath, true).FirstOrDefault(pair => pair.Key != RootType).Value;
                ParameterExpression arrayParameter = null;
                var itemType = array == null ? null : array.Type.GetItemType();
                if(array != null)
                {
                    arrayParameter = Expression.Variable(itemType.MakeArrayType());
                    var arrayEach = Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(itemType), new[] {array});
                    aliases.Add(new KeyValuePair<Expression, Expression>(Expression.ArrayIndex(arrayParameter, indexParameter), arrayEach));
                    aliases.Add(new KeyValuePair<Expression, Expression>(indexParameter, Expression.Call(null, MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(itemType), new Expression[] {arrayEach})));
                    array = array.ResolveAliases(aliases);
                }
                var childResult = new List<Expression>();
                child.BuildTreeMutator(edges, root, item, childParameter, aliases, childResult, visitedNodes, processedNodes, globalResult);
                aliases.RemoveAt(aliases.Count - 1);
                aliases.RemoveAt(aliases.Count - 1);
                if(array != null)
                {
                    aliases.RemoveAt(aliases.Count - 1);
                    aliases.RemoveAt(aliases.Count - 1);
                }
                if(childResult.Count > 0)
                {
                    childResult.Add(childParameter);
                    var action = Expression.Block(new ParameterExpression[] {}, childResult);
                    var forEach = CacheExternalExpressions(action,
                                                           exp => Expression.Call(null, forEachMethod.MakeGenericMethod(child.NodeType), new[] {path, Expression.Lambda(exp, new[] {childParameter, indexParameter})}),
                                                           childParameter, indexParameter);
                    Expression current;
                    if(array == null)
                        current = forEach;
                    else
                    {
                        Expression assign = Expression.Assign(arrayParameter, Expression.Call(toArrayMethod.MakeGenericMethod(itemType), new[] {array}));
                        Expression destArrayIsNull = Expression.ReferenceEqual(path, Expression.Constant(null, path.Type));
                        Expression lengthsAreDifferent = Expression.NotEqual(Expression.ArrayLength(path), Expression.ArrayLength(arrayParameter));
                        var temp = Expression.Parameter(path.Type);
                        Expression resizeIfNeeded = Expression.IfThen(
                            lengthsAreDifferent,
                            Expression.IfThenElse(destArrayIsNull,
                                                  Expression.Assign(path, Expression.NewArrayBounds(child.NodeType, Expression.ArrayLength(arrayParameter))),
                                                  Expression.Block(new[] {temp}, new Expression[]
                                                      {
                                                          Expression.Assign(temp, path),
                                                          Expression.Call(arrayResizeMethod.MakeGenericMethod(child.NodeType), temp, Expression.ArrayLength(arrayParameter)),
                                                          Expression.Assign(path, temp)
                                                      })
                                ));
                        current = Expression.Block(new[] {arrayParameter}, new[] {assign, resizeIfNeeded, forEach});
                    }
                    localResult.Add(current);
                }
            }
            else
                throw new InvalidOperationException();
        }

        private void BuildTreeMutator(Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, Expression fullPath, Expression path, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult,
                                      HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult)
        {
            if(edges != null && edges.Count != 0)
                BuildTreeMutator(edges.Pop(), edges, root, fullPath, path, aliases, localResult, visitedNodes, processedNodes, globalResult);
            else
            {
                BuildNodeMutator(root, path, aliases, localResult, visitedNodes, processedNodes, globalResult);
                foreach(DictionaryEntry entry in children)
                    BuildTreeMutator((ModelConfigurationEdge)entry.Key, edges, root, fullPath, path, aliases, localResult, visitedNodes, processedNodes, globalResult);
            }
        }

        private void BuildNodeMutator(ModelConfigurationNode root, Expression path, List<KeyValuePair<Expression, Expression>> aliases,
                                      List<Expression> localResult, HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult)
        {
            if(visitedNodes.Contains(this))
            {
                if(!processedNodes.Contains(this))
                    throw new FoundCyclicDependencyException("A cycle encountered started at '" + Path + "'");
                return;
            }
            visitedNodes.Add(this);
            var mutators = this.mutators.Where(mutator => mutator.Value is AutoEvaluatorConfiguration).OrderBy(mutator => mutator.Value is DisableIfConfiguration ? 1 : 0).ToArray();
            foreach(var mutator in mutators)
            {
                foreach(var dependency in mutator.Value.Dependencies ?? new LambdaExpression[0])
                {
                    ModelConfigurationNode child;
                    if(!Root.Traverse(dependency.Body, root, out child, false))
                        throw new FoundExternalDependencyException("Unable to build mutator for the subtree '" + Path + "' due to the external dependency '" + dependency + "'");
                    if(child != null && child != this)
                    {
                        var edges = new Stack<ModelConfigurationEdge>();
                        var node = child;
                        while(node != root)
                        {
                            edges.Push(node.Edge);
                            node = node.Parent;
                        }
                        root.BuildTreeMutator(edges, root, aliases.First().Value, aliases.First().Key, new List<KeyValuePair<Expression, Expression>> {aliases.First()}, globalResult, visitedNodes, processedNodes, globalResult);
                    }
                }
            }
            localResult.AddRange(mutators.Select(mutator => ((AutoEvaluatorConfiguration)mutator.Value).Apply(path, aliases)).Where(expression => expression != null));
            processedNodes.Add(this);
        }

        private ModelConfigurationNode GetChild(ModelConfigurationEdge edge, Type childType, bool create)
        {
            var child = (ModelConfigurationNode)children[edge];
            if(child == null && create)
            {
                lock(childrenLock)
                {
                    child = (ModelConfigurationNode)children[edge];
                    if(child == null)
                    {
                        Expression path;
                        if(edge.Value is int)
                            path = Expression.ArrayIndex(Path, Expression.Constant((int)edge.Value));
                        else if(edge.Value is PropertyInfo || edge.Value is FieldInfo)
                            path = Expression.MakeMemberAccess(Path, (MemberInfo)edge.Value);
                        else if(ReferenceEquals(edge.Value, MutatorsHelperFunctions.EachMethod))
                            path = Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(childType), new[] {Path});
                        else
                            throw new InvalidOperationException();
                        child = new ModelConfigurationNode(RootType, childType, Root, this, edge, path);
                        children[edge] = child;
                    }
                }
            }
            return child;
        }

        private ModelConfigurationNode Root { get; set; }
        private ModelConfigurationNode Parent { get; set; }
        private ModelConfigurationEdge Edge { get; set; }
        private readonly List<KeyValuePair<Expression, MutatorConfiguration>> mutators;

        private static readonly MethodInfo forEachMethod = ((MethodCallExpression)((Expression<Action<bool[]>>)(arr => MutatorsHelperFunctions.ForEach(arr, null))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo forEachReadonlyMethod = ((MethodCallExpression)((Expression<Action<IEnumerable<int>>>)(enumerable => MutatorsHelperFunctions.ForEach(enumerable, null))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo toArrayMethod = ((MethodCallExpression)((Expression<Func<int[], int[]>>)(ints => ints.ToArray())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo arrayResizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 5))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo treeAddValidationResultMethod = ((MethodCallExpression)((Expression<Action<ValidationResultTreeNode>>)(tree => tree.AddValidationResult(null, null))).Body).Method;
        private static readonly MethodInfo listAddValidationResultMethod = ((MethodCallExpression)((Expression<Action<List<ValidationResult>>>)(list => list.Add(null))).Body).Method;
        private static readonly ConstructorInfo listValidationResultConstructor = ((NewExpression)((Expression<Func<List<ValidationResult>>>)(() => new List<ValidationResult>())).Body).Constructor;
        private static readonly ConstructorInfo formattedValidationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult, FormattedValidationResult>>)(o => new FormattedValidationResult(o, null, null, 0))).Body).Constructor;

        private readonly Hashtable children = new Hashtable();

        private readonly object childrenLock = new object();

        private class ZzzNode
        {
            public ZzzNode Traverse(Expression exp)
            {
                var edge = new ExpressionWrapper(exp, false);
                ZzzNode child;
                if(!children.TryGetValue(edge, out child))
                    children.Add(edge, child = new ZzzNode());
                return child;
            }

            public void BuildValidator(IPathFormatter pathFormatter, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, ParameterExpression result, ParameterExpression priority, List<Expression> validationResults)
            {
                foreach(var pair in mutators)
                    BuildNodeValidator(pathFormatter, pair.Key, pair.Value, root, aliases, result, priority, validationResults);
                foreach(var pair in children)
                {
                    var edge = pair.Key.Expression;
                    var child = pair.Value;

                    var array = ((MethodCallExpression)edge).Arguments[0];
                    LambdaExpression predicate = null;
                    var resolvedArray = array.ResolveAliases(aliases);
                    var itemType = resolvedArray.Type.GetItemType();
                    var childParameter = Expression.Parameter(itemType);
                    var indexParameter = Expression.Parameter(typeof(int));
                    var item = Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(itemType), new[] {array});
                    var index = Expression.Call(null, MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(itemType), new Expression[] {item});
                    aliases.Add(new KeyValuePair<Expression, Expression>(childParameter, item));
                    aliases.Add(new KeyValuePair<Expression, Expression>(indexParameter, index));
                    if(!resolvedArray.Type.IsArray)
                    {
                        // Filtered array
                        if(resolvedArray.NodeType == ExpressionType.Call)
                        {
                            var methodCallExpression = (MethodCallExpression)resolvedArray;
                            if(methodCallExpression.Method.IsWhereMethod())
                            {
                                resolvedArray = methodCallExpression.Arguments[0];
                                predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                            }
                        }
                    }
                    var childValidationResults = new List<Expression>();
                    child.BuildValidator(pathFormatter, root, aliases, result, priority, childValidationResults);
                    aliases.RemoveAt(aliases.Count - 1);
                    aliases.RemoveAt(aliases.Count - 1);
                    if(childValidationResults.Count > 0)
                    {
                        Expression action = Expression.Block(new ParameterExpression[] {}, childValidationResults);
                        if(predicate != null)
                        {
                            var condition = Expression.Lambda(childParameter, childParameter).Merge(predicate).Body;
                            action = Expression.IfThen(Expression.Equal(Expression.Convert(condition, typeof(bool?)), Expression.Constant(true, typeof(bool?))), action);
                        }
                        var forEach = CacheExternalExpressions(action,
                                                               exp => Expression.Call(null, forEachReadonlyMethod.MakeGenericMethod(itemType), new[] {resolvedArray, Expression.Lambda(exp, new[] {childParameter, indexParameter})}),
                                                               childParameter, indexParameter);
                        validationResults.Add(forEach);
                    }
                }
            }

            public readonly List<KeyValuePair<Expression, List<MutatorConfiguration>>> mutators = new List<KeyValuePair<Expression, List<MutatorConfiguration>>>();
            public readonly Dictionary<ExpressionWrapper, ZzzNode> children = new Dictionary<ExpressionWrapper, ZzzNode>();

            private static void BuildNodeValidator(IPathFormatter pathFormatter,
                                                   Expression path,
                                                   List<MutatorConfiguration> mutators,
                                                   ModelConfigurationNode root,
                                                   List<KeyValuePair<Expression, Expression>> aliases,
                                                   ParameterExpression result,
                                                   ParameterExpression priority,
                                                   List<Expression> validationResults)
            {
                if(mutators.All(mutator => !(mutator is ValidatorConfiguration)))
                    return;
                Expression isDisabled = null;
                foreach(var mutator in mutators)
                {
                    var disableIfConfiguration = mutator as DisableIfConfiguration;
                    if(disableIfConfiguration == null) continue;
                    CheckDependencies(root, disableIfConfiguration);
                    var current = (disableIfConfiguration).GetCondition(aliases);
                    if(current != null)
                        isDisabled = isDisabled == null ? current : Expression.OrElse(current, isDisabled);
                }
                Expression value = Expression.Convert(path.ResolveAliases(aliases), typeof(object));

                var firstAlias = new List<KeyValuePair<Expression, Expression>> {aliases.First()};
                var aliasesInTermsOfFirst = aliases.Count > 1 ? aliases.Skip(1).ToList() : new List<KeyValuePair<Expression, Expression>>();
                aliasesInTermsOfFirst = aliasesInTermsOfFirst.Select(pair => new KeyValuePair<Expression, Expression>(pair.Key, pair.Value.ResolveAliases(firstAlias))).ToList();

                var chains = path.CutToChains(true, true).GroupBy(exp => new ExpressionWrapper(exp, false)).Select(grouping => grouping.Key.Expression.ResolveAliases(firstAlias)).ToArray();
                var indexes = new Expression[aliasesInTermsOfFirst.Count / 2];
                for(var i = 0; i < indexes.Length; ++i)
                    indexes[i] = aliasesInTermsOfFirst[i * 2 + 1].Key;
                var eachesResolver = new EachesResolver(indexes);
                Expression cutChains = Expression.NewArrayInit(typeof(string[]), chains.Select(expression => eachesResolver.Visit(expression).ResolveArrayIndexes()));
                Expression formattedChains;

                if(pathFormatter == null)
                    formattedChains = Expression.Constant(null, typeof(MultiLanguageTextBase));
                else
                {
                    formattedChains = pathFormatter.GetFormattedPath(chains);
                    formattedChains = formattedChains.ResolveAliases(aliasesInTermsOfFirst);
                }

                var localResults = new List<Expression>();
                foreach(var validator in mutators.Where(mutator => mutator is ValidatorConfiguration).Cast<ValidatorConfiguration>())
                {
                    CheckDependencies(root, validator);
                    var current = validator.Apply(aliases);
                    if(current != null)
                    {
                        var currentValidationResult = Expression.Variable(typeof(ValidationResult));
                        Expression addValidationResult = Expression.Call(result, treeAddValidationResultMethod, new[] {Expression.New(formattedValidationResultConstructor, currentValidationResult, value, formattedChains, priority), cutChains});
                        Expression validationResultIsNotNull = Expression.NotEqual(currentValidationResult, Expression.Constant(null, typeof(ValidationResult)));
                        Expression validationResultIsNotOk = Expression.NotEqual(Expression.Property(currentValidationResult, typeof(ValidationResult).GetProperty("Type", BindingFlags.Instance | BindingFlags.Public)), Expression.Constant(ValidationResultType.Ok));
                        Expression condition = Expression.IfThen(Expression.AndAlso(validationResultIsNotNull, validationResultIsNotOk), addValidationResult);
                        localResults.Add(Expression.IfThen(Expression.Not(Expression.Property(result, validationResultTreeNodeExhaustedProperty)), Expression.Block(new[] {currentValidationResult}, Expression.Assign(currentValidationResult, current), condition)));
                    }
                }
                if(isDisabled == null)
                    validationResults.AddRange(localResults);
                else
                {
                    Expression test = Expression.NotEqual(Expression.Convert(isDisabled, typeof(bool?)), Expression.Constant(true, typeof(bool?)));
                    validationResults.Add(Expression.IfThen(test, Expression.Block(new ParameterExpression[] {}, localResults)));
                }
            }

            private static readonly PropertyInfo validationResultTreeNodeExhaustedProperty = (PropertyInfo)((MemberExpression)((Expression<Func<ValidationResultTreeNode, bool>>)(node => node.Exhausted)).Body).Member;
        }
    }

    [DebuggerDisplay("{Value}")]
    public class ModelConfigurationEdge
    {
        public ModelConfigurationEdge(object value)
        {
            Value = value;
        }

        public override int GetHashCode()
        {
            if(Value is int)
                return (int)Value;
            return ((MemberInfo)Value).MetadataToken;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(this, obj))
                return true;
            if(ReferenceEquals(this, null) || ReferenceEquals(obj, null))
                return ReferenceEquals(this, null) && ReferenceEquals(obj, null);
            var other = obj as ModelConfigurationEdge;
            if(ReferenceEquals(other, null))
                return false;
            if(Value is int || other.Value is int)
                return Value is int && other.Value is int && (int)Value == (int)other.Value;
            if(((MemberInfo)Value).Module != ((MemberInfo)(other.Value)).Module)
                return false;
            return ((MemberInfo)Value).MetadataToken == ((MemberInfo)other.Value).MetadataToken;
        }

        public object Value { get; private set; }
        public static readonly ModelConfigurationEdge Each = new ModelConfigurationEdge(MutatorsHelperFunctions.EachMethod);
    }
}