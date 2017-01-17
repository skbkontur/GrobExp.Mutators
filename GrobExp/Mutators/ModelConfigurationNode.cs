using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using GrEmit.Utils;

using GrobExp.Compiler;
using GrobExp.Mutators.Aggregators;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Exceptions;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class MutatorWithPath
    {
        public Expression PathToNode { get; set; }
        public Expression PathToMutator { get; set; }
        public MutatorConfiguration Mutator { get; set; }
    }

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

        public LambdaExpression BuildTreeValidator(IPathFormatter pathFormatter)
        {
            var allMutators = new Dictionary<ExpressionWrapper, List<MutatorConfiguration>>();
            GetMutators(allMutators);

            var root = new ZzzNode();
            foreach(var pair in allMutators)
            {
                var arrays = GetArrays(RootType, pair.Key.Expression, pair.Value);
                var node = arrays.Aggregate(root, (current, array) => current.Traverse(array));
                node.mutators.Add(new KeyValuePair<Expression, List<MutatorConfiguration>>(pair.Key.Expression, pair.Value));
            }

            var parameter = Parent == null ? (ParameterExpression)Path : Expression.Parameter(NodeType, NodeType.Name);
            var treeRootType = ValidationResultTreeNodeBuilder.BuildType(parameter.Type);
            var result = Expression.Parameter(typeof(ValidationResultTreeNode), "tree");
            var priority = Expression.Parameter(typeof(int), "priority");
            var aliases = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, Path)};

            root = GetArrays(RootType, Path, new MutatorConfiguration[0]).Aggregate(root, (current, array) => current.Traverse(array));

            var validationResults = new List<Expression>();
            root.BuildValidator(pathFormatter, this == Root ? null : this, aliases, new Dictionary<ParameterExpression, ExpressionPathsBuilder.SinglePaths>(), result, treeRootType, priority, validationResults);

            //validationResults = validationResults.Select(exp => ExtractLoopInvariantFatExpressions(exp, new []{parameter}, expression => expression)).ToList();
            validationResults = validationResults.SplitToBatches(parameter, result, priority);

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
            body = ExtractLoopInvariantFatExpressions(body, new[] {parameter}, expression => expression);
            var lambda = Expression.Lambda(body, parameter, result, priority);
            return lambda;
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

        public LambdaExpression BuildTreeMutator()
        {
            return BuildTreeMutator(new List<ParameterExpression> {Parent == null ? (ParameterExpression)Path : Expression.Parameter(NodeType, NodeType.Name)});
        }

        public LambdaExpression BuildTreeMutator(Type type)
        {
            return BuildTreeMutator(new List<ParameterExpression>
                {
                    Parent == null ? (ParameterExpression)Path : Expression.Parameter(NodeType, NodeType.Name),
                    Expression.Parameter(type, type.Name)
                });
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

        private static Expression TrimTailToEachOrCurrent(Expression exp)
        {
            var shards = exp.SmashToSmithereens();
            for(var i = shards.Length - 1; i > 0; --i)
            {
                if(shards[i].NodeType != ExpressionType.Call)
                    continue;
                var method = ((MethodCallExpression)shards[i]).Method;
                if(method.IsCurrentMethod() || method.IsEachMethod())
                    return shards[i];
            }
            return exp;
        }

        public void AddMutatorSmart(LambdaExpression path, MutatorConfiguration mutator)
        {
            path = (LambdaExpression)path.Simplify();
            LambdaExpression filter;
            var simplifiedPath = SimplifyPath(path, out filter);
            mutator = mutator.ResolveAliases(CreateAliasesResolver(simplifiedPath.Body, path.Body));
            Traverse(simplifiedPath.Body, true).AddMutator(path.Body, filter == null ? mutator : mutator.If(filter));
        }

        public void AddMutator(MutatorConfiguration mutator)
        {
            if(IsUncoditionalSetter(mutator))
            {
                for(var i = 0; i < mutators.Count; ++i)
                {
                    if(IsUncoditionalSetter(mutators[i].Value) && ExpressionEquivalenceChecker.Equivalent(Path, mutators[i].Key, false, false))
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
            if(IsUncoditionalSetter(mutator))
            {
                for(var i = 0; i < mutators.Count; ++i)
                {
                    if(IsUncoditionalSetter(mutators[i].Value) && ExpressionEquivalenceChecker.Equivalent(path, mutators[i].Key, false, false))
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
            var allMutators = new List<MutatorWithPath>();
            GetMutatorsWithPath(allMutators);
            return PrintAllMutators(allMutators);
        }

        public static string PrintAllMutators(IEnumerable<MutatorWithPath> mutators)
        {
            var result = new StringBuilder();
            foreach(var group in mutators.GroupBy(pair => new ExpressionWrapper(pair.PathToNode, false)))
            {
                result.AppendLine(group.Key.Expression.ToString());
                foreach(var pair in group)
                {
                    result.Append("    PATH: ");
                    result.AppendLine(pair.PathToMutator.ToString());
                    result.Append("    MUTATOR: ");
                    result.AppendLine(pair.Mutator.ToString());
                    result.AppendLine();
                }
            }
            return result.ToString();
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

        private void GetMutatorsWithPath(List<MutatorWithPath> result)
        {
            result.AddRange(GetMutatorsWithPath());
            foreach(var child in Children)
                child.GetMutatorsWithPath(result);
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
        public const int PriorityShift = 1000;

        private static bool IsUncoditionalSetter(MutatorConfiguration mutator)
        {
            return mutator is EqualsToConfiguration && !(mutator is EqualsToIfConfiguration);
        }

        /// <summary>
        ///     Выкидываем вызовы Select и Where, заменяя их на на Current.
        ///     Из всех Where строим жирную лямбду и скидываем её в filter.
        ///     <code>
        ///     qxx.Data.Where(x => x.EndsWith("GRobas")).Select(x => x.ToArray()).Where(x => x.Length > 0).Each().Current() ->
        ///         qxx.Data.Current().ToArray().Current()
        ///         filter = qxx.Data.Current().EndsWith("GRobas") && qxx.Data.Current().ToArray().Length > 0
        /// </code>
        /// </summary>
        private static LambdaExpression SimplifyPath(LambdaExpression path, out LambdaExpression filter)
        {
            filter = null;
            var shards = path.Body.SmashToSmithereens();
            int i;
            for(i = 0; i < shards.Length; ++i)
            {
                if(shards[i].NodeType == ExpressionType.Call && ((MethodCallExpression)shards[i]).Method.DeclaringType == typeof(Enumerable))
                    break;
            }
            if(i >= shards.Length)
                return path;
            var result = shards[i - 1];
            var currents = 0;
            for(; i < shards.Length; ++i)
            {
                var shard = shards[i];
                switch(shard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    result = Expression.MakeMemberAccess(result, ((MemberExpression)shard).Member);
                    break;
                case ExpressionType.ArrayIndex:
                    result = Expression.ArrayIndex(result, ((BinaryExpression)shard).Right);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)shard;
                    var method = methodCallExpression.Method;
                    if(method.DeclaringType == typeof(Enumerable))
                    {
                        switch(method.Name)
                        {
                        case "Select":
                            // Substitute call to Select method with Current
                            // arr.Select(x => x.y) -> arr.Current().y
                            var selector = (LambdaExpression)methodCallExpression.Arguments[1];
                            result =
                                Expression.Lambda(
                                    Expression.Call(
                                        MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(result.Type.GetItemType()), result), path.Parameters)
                                          .Merge(selector).Body;
                            ++currents;
                            break;
                        case "Where":
                            // Remove call to Where, saving it to filter
                            // arr.Where(x => x.y > 0) ->
                            // result := arr
                            // filter := filter && (arr.Current().y > 0)
                            //
                            // If we had Select call replaced by Current, new Current call is not added
                            // arr.Select(x => x.y).Where(y => y.z > 0) -> arr.Current().y.Where(y => y.z > 0) ->
                            // result = arr.Current().y
                            // filter := filter && arr.Current().y.z > 0
                            var predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                            var callExpression = result.Type == predicate.Parameters[0].Type
                                                     ? result
                                                     : Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(result.Type.GetItemType()), result);
                            var currentFilter = Expression.Lambda(callExpression, path.Parameters).Merge(predicate);
                            filter = filter == null ? currentFilter : filter.AndAlso(currentFilter, false);
                            break;
                        default:
                            throw new NotSupportedException(string.Format("Method '{0}' is not supported", method));
                        }
                    }
                    else if(method.DeclaringType == typeof(MutatorsHelperFunctions))
                    {
                        switch(method.Name)
                        {
                        case "Current":
                        case "Each":
                            // Remove Each/Current call if it is added before by processing of Select/Where calls.
                            --currents;
                            if(currents < 0)
                            {
                                result = Expression.Call(method.GetGenericMethodDefinition().MakeGenericMethod(result.Type.GetItemType()), result);
                                ++currents;
                            }
                            break;
                        default:
                            throw new NotSupportedException(string.Format("Method '{0}' is not supported", method));
                        }
                    }
                    else
                        throw new NotSupportedException(string.Format("Method '{0}' is not supported", method));
                    break;
                case ExpressionType.ArrayLength:
                    result = Expression.ArrayLength(result);
                    break;
                case ExpressionType.Convert:
                    result = Expression.Convert(result, shard.Type);
                    break;
                default:
                    throw new NotSupportedException(string.Format("Node type '{0}' is not valid at this point", shard.NodeType));
                }
            }
            return Expression.Lambda(result, path.Parameters);
        }

        private static bool Equivalent(Expression first, Expression second)
        {
            if(first.NodeType != second.NodeType)
                return false;
            switch(first.NodeType)
            {
            case ExpressionType.MemberAccess:
                return ((MemberExpression)first).Member == ((MemberExpression)second).Member;
            case ExpressionType.Call:
                return ((MethodCallExpression)first).Method == ((MethodCallExpression)second).Method;
            case ExpressionType.ArrayLength:
                return true;
            case ExpressionType.ArrayIndex:
                var firstIndex = ((BinaryExpression)first).Right;
                var secondIndex = ((BinaryExpression)second).Right;
                if(firstIndex.NodeType != ExpressionType.Constant)
                    throw new NotSupportedException(string.Format("Node type '{0}' is not supported", firstIndex.NodeType));
                if(secondIndex.NodeType != ExpressionType.Constant)
                    throw new NotSupportedException(string.Format("Node type '{0}' is not supported", secondIndex.NodeType));
                return ((ConstantExpression)firstIndex).Value == ((ConstantExpression)secondIndex).Value;
            case ExpressionType.Convert:
                return first.Type == second.Type;
            default:
                throw new NotSupportedException(string.Format("Node type '{0}' is not supported", first.NodeType));
            }
        }

        private static AliasesResolver CreateAliasesResolver(Expression simplifiedPath, Expression path)
        {
            var simplifiedPathShards = simplifiedPath.SmashToSmithereens();
            var pathShards = path.SmashToSmithereens();
            var i = simplifiedPathShards.Length - 1;
            var j = pathShards.Length - 1;
            while(i > 0 && j > 0)
            {
                if(!Equivalent(simplifiedPathShards[i], pathShards[j]))
                    break;
                --i;
                --j;
            }
            var simplifiedShard = simplifiedPathShards[i];
            var pathShard = pathShards[j];
            // To add alias with CurrentIndex call we need to trim simplifiedShard, 
            // because we replaced Select/Where calls with Current/Each calls from the tail of pathShard
            var cutSimplifiedShard = TrimTailToEachOrCurrent(simplifiedShard);
            return new AliasesResolver(new List<KeyValuePair<Expression, Expression>>
                {
                    new KeyValuePair<Expression, Expression>(simplifiedShard, pathShard),
                    new KeyValuePair<Expression, Expression>(Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(cutSimplifiedShard.Type), cutSimplifiedShard),
                                                             Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(pathShard.Type), pathShard))
                });
        }

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

        private bool Traverse(Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create)
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
        private bool Traverse(Expression path, ModelConfigurationNode subRoot, out ModelConfigurationNode child, bool create, List<KeyValuePair<Expression, Expression>> arrayAliases)
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
        ///     Строит жирный Expression, содержаший все мутации или валидации.
        ///     Все, что нужно после этого - скомпилировать.
        /// </summary>
        /// <param name="parameters">Параметры Expression'а (для конвертации - target и source)</param>
        private LambdaExpression BuildTreeMutator(List<ParameterExpression> parameters)
        {
            var visitedNodes = new HashSet<ModelConfigurationNode>();
            var processedNodes = new HashSet<ModelConfigurationNode>();
            var mutatorExpressions = new List<Expression>();

            //Добавляем алиас для случая, когда билдим жиромутатор для поддерева, чтобы заменить путь до корня на реальный параметр выражения
            var aliases = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameters[0], Path)};
            var invariantParameters = new List<ParameterExpression>();
            if(parameters.Count > 1)
                invariantParameters.Add(parameters[1]);
            BuildTreeMutator(null, this, aliases, mutatorExpressions, visitedNodes, processedNodes, mutatorExpressions, invariantParameters);

            //Оптимизации
            mutatorExpressions = mutatorExpressions.SplitToBatches(parameters.ToArray());
            mutatorExpressions.Add(Expression.Empty());
            Expression body = Expression.Block(mutatorExpressions);
            body = ExtractLoopInvariantFatExpressions(body, invariantParameters, expression => expression);

            //Далее параметры мутаторов подменяются на параметры итогового выражения
            foreach(var actualParameter in body.ExtractParameters())
            {
                var expectedParameter = parameters.Single(p => p.Type == actualParameter.Type);
                if(actualParameter != expectedParameter)
                    body = new ParameterReplacer(actualParameter, expectedParameter).Visit(body);
            }
            var result = Expression.Lambda(body, parameters);
            return result;
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

        private static Expression ExtractLoopInvariantFatExpressions(Expression expression, IEnumerable<ParameterExpression> invariantParameters, Func<Expression, Expression> resultSelector)
        {
            var extractedExpressions = new InvariantCodeExtractor(invariantParameters).Extract(expression);
            if(extractedExpressions.Length == 0)
                return resultSelector(expression);

            var aliases = new Dictionary<Expression, Expression>();
            var variables = new List<ParameterExpression>();
            foreach(var exp in extractedExpressions)
            {
                if(!aliases.ContainsKey(exp))
                {
                    var variable = Expression.Variable(exp.Type);
                    variables.Add(variable);
                    aliases.Add(exp, variable);
                }
            }
            var optimizedExpression = new ExpressionReplacer(aliases).Visit(expression);
            return Expression.Block(variables, aliases.Select(pair => Expression.Assign(pair.Value, pair.Key)).Concat(new[] {resultSelector(optimizedExpression)}));
        }

        private static void CheckDependencies(ModelConfigurationNode root, MutatorConfiguration mutator)
        {
            if(root == null || mutator == null || mutator.Dependencies == null)
                return;
            foreach(var dependency in mutator.Dependencies)
            {
                ModelConfigurationNode child;
                if(!root.Root.Traverse(dependency.Body, root, out child, false))
                    throw new FoundExternalDependencyException("Unable to build validator for the subtree '" + root.Parent + "' due to the external dependency '" + dependency + "'");
            }
        }

        /// <summary>
        ///     Просто выполняет переход по ребру и строит выражения для перечисления массивов и словарей.
        /// </summary>
        private void BuildTreeMutator(ModelConfigurationEdge edge, Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult,
                                      HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            var child = children[edge];
            if(edge.IsMemberAccess || edge.IsArrayIndex || edge.IsConvertation || edge.IsIndexerParams)
                child.BuildTreeMutator(edges, root, aliases, localResult, visitedNodes, processedNodes, globalResult, invariantParameters);
            else if(edge.IsEachMethod)
            {
                var path = Path.ResolveAliases(aliases);
                if(NodeType.IsDictionary())
                    BuildTreeMutatorForDictionary(child, edges, root, aliases, localResult, visitedNodes, processedNodes, path, globalResult, invariantParameters);
                else
                    BuildTreeMutatorForArray(child, edges, root, aliases, localResult, visitedNodes, processedNodes, path, globalResult, invariantParameters);
            }
            else
                throw new InvalidOperationException();
        }

        /// <summary>
        ///     Разворачиваем всякие ичи-хуичи в нормальные циклы посредством хитровыебнутой функции MutatorsHelperFunctions.ForEach
        /// </summary>
        private void BuildTreeMutatorForArray(ModelConfigurationNode child, Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult, HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, Expression path, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            // We want to transform this:
            //   Target(target => target.A.B.Each().S).Set(source => source.T.R.Current().U.S)
            // To the following code:
            //   var array = source.T.R.ToArray();
            //   if(target.A.B == null || target.A.B.Length != source.T.R.Length)
            //   {
            //       if(target.A.B == null)
            //       {
            //           target.A.B = new object[source.T.R.Length];
            //       }
            //       else
            //       {
            //           var tmp = target.A.B;
            //           Array.Resize(ref tmp, source.T.R.Length);
            //           target.A.B = tmp;
            //       }
            //   }
            //   MutatorsHelperFunctions.ForEach(target.A.B, (b, i) =>
            //       {
            //           b.S = array[i].U.S;
            //           return b;
            //       });

            // Create parameters for lambda to use in ForEach method and create aliases for Each() and CurrentIndex()
            var childParameter = Expression.Parameter(child.NodeType, child.NodeType.Name);
            var indexParameter = Expression.Parameter(typeof(int));
            var item = Path.MakeEachCall(child.NodeType);
            var index = item.MakeCurrentIndexCall(child.NodeType);
            aliases.Add(new KeyValuePair<Expression, Expression>(childParameter, item));
            aliases.Add(new KeyValuePair<Expression, Expression>(indexParameter, index));

            // Find a source array to take values from
            // todo ich: почему только первый?
            var arrays = GetArrays(Path);
            var canonizedFullPath = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(Path);
            var array = arrays.FirstOrDefault(pair => !new ExpressionWrapper(pair.Value, false).Equals(new ExpressionWrapper(canonizedFullPath, false))).Value;

            ParameterExpression arrayParameter = null;
            var itemType = array == null ? null : array.Type.GetItemType();
            if(array != null)
            {
                // If any array found - create a variable for it and aliases for Each() and CurrentIndex()
                arrayParameter = Expression.Variable(itemType.MakeArrayType());
                invariantParameters.Add(array.ExtractParameters().Single());
                invariantParameters.Add(arrayParameter);
                invariantParameters.Add(indexParameter);
                var arrayEach = array.MakeEachCall(itemType);
                var arrayCurrentIndex = arrayEach.MakeCurrentIndexCall(itemType);
                aliases.Add(new KeyValuePair<Expression, Expression>(Expression.ArrayIndex(arrayParameter, indexParameter), arrayEach));
                aliases.Add(new KeyValuePair<Expression, Expression>(indexParameter, arrayCurrentIndex));
                array = array.ResolveAliases(aliases).EliminateLinq();
            }

            // Build mutators in subtree into a separate list, to put them inside ForEach lambda
            var childResult = new List<Expression>();
            child.BuildTreeMutator(edges, root, aliases, childResult, visitedNodes, processedNodes, globalResult, invariantParameters);

            // Remove all created aliases
            aliases.RemoveAt(aliases.Count - 1);
            aliases.RemoveAt(aliases.Count - 1);
            if(array != null)
            {
                invariantParameters.RemoveAt(invariantParameters.Count - 1);
                aliases.RemoveAt(aliases.Count - 1);
                aliases.RemoveAt(aliases.Count - 1);
            }

            if(childResult.Count > 0)
            {
                // ForEach method requires mutators block to return the target item
                childResult.Add(childParameter);

                // Optimization of block size to avoid big functions
                var action = Expression.Block(childResult.SplitToBatches());
                // Make a call to MutatorsHelperFunctions.ForEach
                var forEach = ExtractLoopInvariantFatExpressions(action, invariantParameters, exp => Expression.Call(null, forEachMethod.MakeGenericMethod(child.NodeType), new[] {path, Expression.Lambda(exp, childParameter, indexParameter)}));
                Expression result;
                if(array == null)
                    result = forEach;
                else
                {
                    // If we had found an array
                    // Assign it to the array variable with a ToArray() call.
                    Expression assign = Expression.Assign(arrayParameter, Expression.Call(toArrayMethod.MakeGenericMethod(itemType), new[] {array}));
                    // Add a check if target array is null or needs to be resized
                    var resizeIfNeeded = CreateOrResizeArrayIfNeeded(child, path, arrayParameter);
                    result = Expression.Block(new[] {arrayParameter}, assign, resizeIfNeeded, forEach);
                }
                localResult.Add(result);
            }
            if(array != null)
            {
                invariantParameters.RemoveAt(invariantParameters.Count - 1);
                invariantParameters.RemoveAt(invariantParameters.Count - 1);
            }
        }

        private static Expression CreateOrResizeArrayIfNeeded(ModelConfigurationNode child, Expression path, ParameterExpression arrayParameter)
        {
            Expression destArrayIsNull = Expression.ReferenceEqual(path, Expression.Constant(null, path.Type));
            Expression resizeIfNeeded;
            if(path.Type.IsArray)
            {
                Expression lengthsAreDifferent = Expression.OrElse(destArrayIsNull, Expression.NotEqual(Expression.ArrayLength(path), Expression.ArrayLength(arrayParameter)));
                var temp = Expression.Parameter(path.Type, path.Type.Name);
                resizeIfNeeded = Expression.IfThen(
                    lengthsAreDifferent,
                    Expression.IfThenElse(destArrayIsNull,
                                          path.Assign(Expression.NewArrayBounds(child.NodeType, Expression.ArrayLength(arrayParameter))),
                                          Expression.Block(new[] {temp}, Expression.Assign(temp, path), Expression.Call(arrayResizeMethod.MakeGenericMethod(child.NodeType), temp, Expression.ArrayLength(arrayParameter)), path.Assign(temp))
                        ));
            }
            else if(path.Type.IsGenericType && path.Type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Expression lengthsAreDifferent = Expression.NotEqual(Expression.Property(path, "Count"), Expression.ArrayLength(arrayParameter));
                var expressions = new List<Expression>();
                if(path.NodeType == ExpressionType.MemberAccess && CanWrite(((MemberExpression)path).Member))
                    expressions.Add(Expression.IfThen(destArrayIsNull, Expression.Assign(path, Expression.New(path.Type.GetConstructor(new[] {typeof(int)}), Expression.ArrayLength(arrayParameter)))));
                expressions.Add(Expression.Call(listResizeMethod.MakeGenericMethod(child.NodeType), path, Expression.ArrayLength(arrayParameter)));
                resizeIfNeeded = Expression.IfThen(lengthsAreDifferent, Expression.Block(expressions));
            }
            else throw new NotSupportedException("Enumeration over '" + path.Type + "' is not supported");
            return resizeIfNeeded;
        }

        /// <summary>
        ///     Разворачиваем всякие ичи-хуичи в нормальные циклы посредством хитровыебнутой функции ForEachOverDictionary
        /// </summary>
        private void BuildTreeMutatorForDictionary(ModelConfigurationNode child, Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult, HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, Expression path, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            // Единственное отличие от BuildTreeMutatorForArray, состоит в том, что нужно отдельно найти мутатор для ключа, чтобы запихать его в ForEachOverDictionary
            var arguments = child.NodeType.GetGenericArguments();
            var destKeyType = arguments[0];
            var destValueType = arguments[1];
            var destValueParameter = Expression.Variable(destValueType);
            var destKeyParameter = Expression.Variable(destKeyType);
            var destArrayEach = Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(child.NodeType), Path);
            var destValue = Expression.Property(destArrayEach, "Value");
            var destKey = Expression.Property(destArrayEach, "Key");

            aliases.Add(new KeyValuePair<Expression, Expression>(destValueParameter, destValue));
            aliases.Add(new KeyValuePair<Expression, Expression>(destKeyParameter, destKey));

            // todo ich: почему только первый?
            var array = GetArrays(Path).Single().Value;
            var itemType = array.Type.GetItemType();
            arguments = itemType.GetGenericArguments();
            var sourceKeyType = arguments[0];
            var sourceValueType = arguments[1];
            var sourceValueParameter = Expression.Variable(sourceValueType);
            var sourceKeyParameter = Expression.Variable(sourceKeyType);
            var sourceArrayEach = Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(itemType), array);
            var sourceValue = Expression.Property(sourceArrayEach, "Value");
            var sourceKey = Expression.Property(sourceArrayEach, "Key");
            invariantParameters.Add(sourceValueParameter);
            invariantParameters.Add(sourceKeyParameter);

            aliases.Add(new KeyValuePair<Expression, Expression>(sourceValueParameter, sourceValue));
            aliases.Add(new KeyValuePair<Expression, Expression>(sourceKeyParameter, sourceKey));
            array = array.ResolveAliases(aliases).EliminateLinq();

            var childResult = new List<Expression>();
            child.BuildTreeMutator(edges, root, aliases, childResult, visitedNodes, processedNodes, globalResult, invariantParameters);

            invariantParameters.RemoveAt(invariantParameters.Count - 1);
            invariantParameters.RemoveAt(invariantParameters.Count - 1);
            aliases.RemoveAt(aliases.Count - 1);
            aliases.RemoveAt(aliases.Count - 1);
            aliases.RemoveAt(aliases.Count - 1);
            aliases.RemoveAt(aliases.Count - 1);
            if(childResult.Count > 0)
            {
                var indexOfKeyAssigner = -1;
                for(var i = 0; i < childResult.Count; ++i)
                {
                    if(childResult[i].NodeType == ExpressionType.Assign && ((BinaryExpression)childResult[i]).Left == destKeyParameter)
                    {
                        indexOfKeyAssigner = i;
                        break;
                    }
                }
                if(indexOfKeyAssigner < 0)
                    throw new InvalidOperationException("Key selector is missing");
                var keySelector = Expression.Lambda(((BinaryExpression)childResult[indexOfKeyAssigner]).Right, sourceKeyParameter);
                childResult.RemoveAt(indexOfKeyAssigner);

                childResult.Add(destValueParameter);
                var action = Expression.Block(childResult.SplitToBatches());
                var forEach = ExtractLoopInvariantFatExpressions(action, invariantParameters, exp => Expression.Call(null, forEachOverDictionaryMethod.MakeGenericMethod(sourceKeyType, destKeyType, sourceValueType, destValueType), array, path, keySelector, Expression.Lambda(exp, sourceValueParameter, destValueParameter)));

                var extendedDict = CreateDictIfNull(path);
                localResult.Add(Expression.Block(new[] {extendedDict, forEach}));
            }
        }

        private static Expression CreateDictIfNull(Expression path)
        {
            if(path.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = (MemberExpression)path;
                var lazyType = typeof(Lazy<>).MakeGenericType(path.Type);
                if(memberExpression.Member == lazyType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public))
                {
                    var lazyConstructor = lazyType.GetConstructor(new[] {typeof(Func<>).MakeGenericType(path.Type)});
                    return Expression.IfThen(
                        Expression.ReferenceEqual(memberExpression.Expression, Expression.Constant(null, lazyType)),
                        Expression.Assign(memberExpression.Expression,
                                          Expression.New(lazyConstructor, Expression.Lambda(Expression.New(path.Type)))));
                }
            }
            return Expression.IfThen(
                Expression.ReferenceEqual(path, Expression.Constant(null, path.Type)),
                Expression.Assign(path, Expression.New(path.Type)));
        }

        private static bool CanWrite(MemberInfo member)
        {
            return member.MemberType == MemberTypes.Field || (member.MemberType == MemberTypes.Property && ((PropertyInfo)member).CanWrite);
        }

        /// <param name="edges">Рёбра, ведущие до зависимости. Используем их, чтобы не запускать повторно на этом пути <see cref="BuildNodeMutator">BuildNodeMutator</see>, который иначе сдохнет "найдя циклическую зависимость", и не обходить других детей. </param>
        /// <param name="root">Корень поддерева, для которого мы строим Expression.</param>
        /// <param name="aliases">Алиасы для массивов и их текущих индексов + алиас на корень.</param>
        /// <param name="localResult">Локальный список построенных Expression-ов. Используется, когда нам нужно получить все Expression-ы в текущем поддереве (например, при построении циклов по массивам).</param>
        /// <param name="visitedNodes">Список посещённых нод (для которых мы вызвали <see cref="BuildNodeMutator">BuildNodeMutator</see>). Используется для поиска циклов в зависимостях.</param>
        /// <param name="processedNodes">Список обработанных нод (для которых мы вызвали <see cref="BuildNodeMutator">BuildNodeMutator</see> и он уже закончился). Используется для поиска циклов в зависимостях.</param>
        /// <param name="globalResult">Общий список, куда складываем все построенные Expression-ы.</param>
        /// <param name="invariantParameters">Список параметров, использующихся в <see cref="ExtractLoopInvariantFatExpressions">ExtractLoopInvariantFatExpressions</see> для оптимизации циклов. Эти параметры - синонимы путей от source</param>
        private void BuildTreeMutator(Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult,
                                      HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            // Если у нас есть ещё рёбра - переходим по ним, 'телепортируясь' к ноде с нужной нам зависимостью.
            if(edges != null && edges.Count != 0)
                BuildTreeMutator(edges.Pop(), edges, root, aliases, localResult, visitedNodes, processedNodes, globalResult, invariantParameters);
            else
            {
                BuildNodeMutator(root, aliases, localResult, visitedNodes, processedNodes, globalResult, invariantParameters);
                foreach(var entry in children)
                    BuildTreeMutator(entry.Key, edges, root, aliases, localResult, visitedNodes, processedNodes, globalResult, invariantParameters);
            }
        }

        private void BuildNodeMutator(ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases,
                                      List<Expression> localResult, HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            var path = Path.ResolveAliases(aliases);
            if(visitedNodes.Contains(this))
            {
                if(!processedNodes.Contains(this))
                    throw new FoundCyclicDependencyException("A cycle encountered started at '" + Path + "'");
                return;
            }
            visitedNodes.Add(this);

            var selfDependentMutators = new List<AutoEvaluatorConfiguration>();
            var otherMutators = new List<AutoEvaluatorConfiguration>();
            foreach(var mutator in mutators.Where(mutator => mutator.Value is AutoEvaluatorConfiguration))
            {
                var selfDependent = false;
                foreach(var dependency in mutator.Value.Dependencies ?? new LambdaExpression[0])
                {
                    ModelConfigurationNode child;
                    // Спускаемся из глобального корня по найденной зависимости.
                    // Важно, чтобы путь проходил через корень поддерва, для которого мы изначально запустили BuildTreeMutator.
                    // Иначе есть какая-то внешняя зависимость, и ничего работать не будет.
                    if(!Root.Traverse(dependency.Body, root, out child, false))
                        throw new FoundExternalDependencyException("Unable to build mutator for the subtree '" + Path + "' due to the external dependency '" + dependency + "'");

                    if(child == null)
                    {
                        // Если у нас в дереве нет вершины соответствующей пути до зависимости, т.е. нет конфигураций для её заполнения,
                        // нужно найти самый длинный префикс этого пути, в котором такая конфигурация есть.
                        var found = false;
                        var shards = dependency.Body.SmashToSmithereens();
                        for(var i = shards.Length - 1; i >= 0; --i)
                        {
                            Root.Traverse(shards[i], root, out child, false);
                            if(child != null && child.mutators.Any(pair => pair.Value is EqualsToConfiguration))
                            {
                                found = true;
                                break;
                            }
                        }
                        if(!found) child = null;
                    }
                    if(child != null && child != this)
                    {
                        // Если зависимости соответствует какая-то вершина в дереве (кроме корня), то надо сначала построить выражения 
                        // для её поддерева, которые должны идти перед выражениями для текущей вершины. 
                        // Потом нужно к ней аккуратно спуститься от корня, не вызывая билд для промежуточных вершин.
                        // Для этого, запоминаем путь до неё в стек рёбер.
                        var edges = new Stack<ModelConfigurationEdge>();
                        var node = child;
                        while(node != root)
                        {
                            edges.Push(node.Edge);
                            node = node.Parent;
                        }
                        // Запускаем билд для зависимости, безо всех текущих алиасов на массивы.
                        root.BuildTreeMutator(edges, root, new List<KeyValuePair<Expression, Expression>> {aliases.First()}, globalResult, visitedNodes, processedNodes, globalResult, invariantParameters);
                    }
                    selfDependent |= child == this;
                }
                (selfDependent ? selfDependentMutators : otherMutators).Add((AutoEvaluatorConfiguration)mutator.Value);
            }
            // Добавляем все выражения мутаторов к результату.
            // При этом важно, чтобы выражения мутаторов, которые зависят от себя, шли после всех остальных. 
            // Например поле заполняется, а потом, при определённом условии реконфигурируется.
            localResult.AddRange(otherMutators.Concat(selfDependentMutators)
                                              .Select(mutator => mutator.Apply(path, aliases).EliminateLinq())
                                              .Where(expression => expression != null));
            processedNodes.Add(this);
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

        private static void Resize<T>(List<T> list, int size)
        {
            // todo emit
            if(list.Count > size)
            {
                while(list.Count > size)
                    list.RemoveAt(list.Count - 1);
            }
            else
            {
                while(list.Count < size)
                    list.Add(default(T));
            }
        }

        private ModelConfigurationNode Root { get; set; }
        private ModelConfigurationNode Parent { get; set; }
        private ModelConfigurationEdge Edge { get; set; }
        private readonly List<KeyValuePair<Expression, MutatorConfiguration>> mutators;

        private static readonly MethodInfo forEachMethod = ((MethodCallExpression)((Expression<Action<bool[]>>)(arr => MutatorsHelperFunctions.ForEach(arr, null))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo forEachReadonlyMethod = ((MethodCallExpression)((Expression<Action<IEnumerable<int>>>)(enumerable => MutatorsHelperFunctions.ForEach(enumerable, null))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo forEachOverDictionaryMethod = ((MethodCallExpression)((Expression<Action<Dictionary<int, int>, Dictionary<int, int>>>)((source, dest) => MutatorsHelperFunctions.ForEach(source, dest, i => i, (x, y) => x + y))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo toArrayMethod = ((MethodCallExpression)((Expression<Func<int[], int[]>>)(ints => ints.ToArray())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo arrayResizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 5))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo listResizeMethod = ((MethodCallExpression)((Expression<Action<List<int>>>)(list => Resize(list, 5))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo listAddValidationResultMethod = ((MethodCallExpression)((Expression<Action<List<ValidationResult>>>)(list => list.Add(null))).Body).Method;
        private static readonly ConstructorInfo listValidationResultConstructor = ((NewExpression)((Expression<Func<List<ValidationResult>>>)(() => new List<ValidationResult>())).Body).Constructor;
        private static readonly ConstructorInfo formattedValidationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult, FormattedValidationResult>>)(o => new FormattedValidationResult(o, null, null, 0))).Body).Constructor;

        private readonly Dictionary<ModelConfigurationEdge, ModelConfigurationNode> children = new Dictionary<ModelConfigurationEdge, ModelConfigurationNode>();

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

            public void BuildValidator(IPathFormatter pathFormatter, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, Dictionary<ParameterExpression, ExpressionPathsBuilder.SinglePaths> paths, ParameterExpression result, Type treeRootType, ParameterExpression priority, List<Expression> validationResults)
            {
                foreach(var pair in mutators)
                    BuildNodeValidator(pathFormatter, pair.Key, pair.Value, root, aliases, paths, result, treeRootType, priority, validationResults);
                foreach(var pair in children)
                {
                    var edge = pair.Key.Expression;
                    var child = pair.Value;

                    var array = ((MethodCallExpression)edge).Arguments[0];
                    LambdaExpression predicate = null;
                    var resolvedArray = array.ResolveAliases(aliases);
                    var itemType = resolvedArray.Type.GetItemType();
                    var item = Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(itemType), array);
                    var index = Expression.Call(null, MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(itemType), item);
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

                    ParameterExpression[] indexes = null;
                    var parameter = Expression.Parameter(itemType);
                    var adjustedResolvedArray = Expression.Call(selectMethod.MakeGenericMethod(itemType, itemType), resolvedArray, Expression.Lambda(parameter, parameter));
                    adjustedResolvedArray = Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(itemType), adjustedResolvedArray);

                    var monster = new LinqEliminator().EliminateAndEnumerate(adjustedResolvedArray, (current, currentIndex, currentIndexes) =>
                        {
                            indexes = currentIndexes;
                            aliases.Add(new KeyValuePair<Expression, Expression>(current, item));
                            aliases.Add(new KeyValuePair<Expression, Expression>(currentIndex, index));
                            var currentPaths = ExpressionPathsBuilder.BuildPaths(adjustedResolvedArray, currentIndexes, paths);
                            //currentPaths.Add(currentIndex);
                            paths.Add(current, currentPaths);

                            var childValidationResults = new List<Expression>();
                            child.BuildValidator(pathFormatter, root, aliases, paths, result, treeRootType, priority, childValidationResults);
                            aliases.RemoveAt(aliases.Count - 1);
                            aliases.RemoveAt(aliases.Count - 1);

                            paths.Remove(current);

                            if(predicate != null)
                            {
                                var condition = Expression.Lambda(current, current).Merge(predicate).Body;
                                for(var i = 0; i < childValidationResults.Count; ++i)
                                {
                                    childValidationResults[i] = Expression.IfThen(
                                        Expression.Equal(
                                            Expression.Convert(condition, typeof(bool?)),
                                            Expression.Constant(true, typeof(bool?))),
                                        childValidationResults[i]);
                                }
                            }

                            for(var i = 0; i < childValidationResults.Count; ++i)
                            {
                                childValidationResults[i] = ExtractLoopInvariantFatExpressions(childValidationResults[i], aliases.Where(p => p.Key is ParameterExpression).Select(p => (ParameterExpression)p.Key), e => e);
                            }
                            return Expression.Block(childValidationResults.SplitToBatches());
                        });

                    if(indexes != null && indexes.Length > 0)
                        monster = Expression.Block(indexes, monster);
                    validationResults.Add(monster);
                }
            }

            private static void BuildNodeValidator(IPathFormatter pathFormatter, Expression path, List<MutatorConfiguration> mutators, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, Dictionary<ParameterExpression, ExpressionPathsBuilder.SinglePaths> paths, ParameterExpression result, Type treeRootType, ParameterExpression priority, List<Expression> validationResults)
            {
                if(mutators.All(mutator => !(mutator is ValidatorConfiguration)))
                    return;
                Expression isDisabled = null;
                foreach(var mutator in mutators)
                {
                    var disableIfConfiguration = mutator as DisableIfConfiguration;
                    if(disableIfConfiguration == null) continue;
                    CheckDependencies(root, disableIfConfiguration);
                    var current = disableIfConfiguration.GetCondition(aliases);
                    if(current != null)
                        isDisabled = isDisabled == null ? current : Expression.OrElse(current, isDisabled);
                }

                var firstAlias = new List<KeyValuePair<Expression, Expression>> {aliases.First()};
                var aliasesInTermsOfFirst = aliases.Count > 1 ? aliases.Skip(1).ToList() : new List<KeyValuePair<Expression, Expression>>();
                aliasesInTermsOfFirst = aliasesInTermsOfFirst.Select(pair => new KeyValuePair<Expression, Expression>(pair.Key, pair.Value.ResolveAliases(firstAlias))).ToList();

                //var eachesResolver = new EachesResolver(new int[aliasesInTermsOfFirst.Count / 2].Select((x, i) => aliasesInTermsOfFirst[i * 2 + 1].Key).ToArray());

                // Replace LINQ methods with cycles to obtain indexes
                ParameterExpression[] currentIndexes;
                //var resolvedPath = eachesResolver.Visit(path.ResolveAliases(firstAlias));
                var currentPath = path.ResolveAliases(aliases);
                var value = Expression.Parameter(typeof(object));
                Expression valueAssignment = Expression.Assign(value, Expression.Convert(new LinqEliminator().Eliminate(currentPath, out currentIndexes), typeof(object)));
                var currentPaths = ExpressionPathsBuilder.BuildPaths(currentPath, currentIndexes, paths);
                var resolvedArrayIndexes = currentPaths.paths.Select(p => new ResolvedArrayIndexes {path = p}).ToArray();

                var chains = path.CutToChains(true, true).GroupBy(exp => new ExpressionWrapper(exp, false)).Select(grouping => grouping.Key.Expression.ResolveAliases(firstAlias)).ToArray();
                //Expression cutChains = Expression.NewArrayInit(typeof(string[]), chains.Select(expression => eachesResolver.Visit(expression).ResolveArrayIndexes()));

                Expression formattedChains = null;

                if(pathFormatter != null)
                {
                    formattedChains = pathFormatter.GetFormattedPath(chains);
                    if(formattedChains != null)
                        formattedChains = formattedChains.ResolveAliases(aliasesInTermsOfFirst);
                }
                if(formattedChains == null)
                {
                    // Default path formatting - simply list all the paths along the object tree
                    if(!(pathFormatter is PathFormatterWrapper))
                        formattedChains = FormatPaths(currentPaths);
                    else
                        formattedChains = Expression.Constant(null, typeof(MultiLanguagePathText));
                }

                var localResults = new List<Expression> {valueAssignment};
                foreach(var validator in mutators.Where(mutator => mutator is ValidatorConfiguration).Cast<ValidatorConfiguration>())
                {
                    CheckDependencies(root, validator);
                    var appliedValidator = validator.Apply(aliases).EliminateLinq();
                    if(appliedValidator == null) continue;
                    var currentValidationResult = Expression.Variable(typeof(ValidationResult));
                    if(validator.Priority < 0)
                        throw new PriorityOutOfRangeException("Validator's priority cannot be less than zero");
                    if(validator.Priority >= PriorityShift)
                        throw new PriorityOutOfRangeException("Validator's priority must be less than " + PriorityShift);
                    var validatorPriority = Expression.Constant(validator.Priority);
                    Expression currentPriority = Expression.AddChecked(Expression.MultiplyChecked(priority, Expression.Constant(PriorityShift)), validatorPriority);
                    // todo вызывать один раз
                    var targetValidationResults = SelectTargetNode(result, treeRootType, resolvedArrayIndexes);
                    var listAddMethod = HackHelpers.GetMethodDefinition<ValidationResultTreeNode>(x => x.AddValidationResult(null));
                    var currentFormattedValidationResult = Expression.New(formattedValidationResultConstructor, currentValidationResult, value, formattedChains, currentPriority);
                    Expression addValidationResult = Expression.Call(targetValidationResults, listAddMethod, currentFormattedValidationResult);
                    Expression validationResultIsNotNull = Expression.NotEqual(currentValidationResult, Expression.Constant(null, typeof(ValidationResult)));
                    Expression validationResultIsNotOk = Expression.NotEqual(Expression.Property(currentValidationResult, typeof(ValidationResult).GetProperty("Type", BindingFlags.Instance | BindingFlags.Public)), Expression.Constant(ValidationResultType.Ok));
                    Expression condition = Expression.IfThen(Expression.AndAlso(validationResultIsNotNull, validationResultIsNotOk), addValidationResult);
                    var localResult = Expression.IfThen(Expression.Not(Expression.Call(MutatorsHelperFunctions.DynamicMethod.MakeGenericMethod(typeof(bool)), Expression.Property(result, validationResultTreeNodeExhaustedProperty))), Expression.Block(new[] {currentValidationResult}, Expression.Assign(currentValidationResult, appliedValidator), condition));
                    localResults.Add(localResult);
                }
                var appliedValidators = Expression.Block(new[] {value}.Concat(currentIndexes), localResults);
                if(isDisabled == null)
                    validationResults.Add(appliedValidators);
                else
                {
                    Expression test = Expression.NotEqual(Expression.Convert(isDisabled, typeof(bool?)), Expression.Constant(true, typeof(bool?)));
                    validationResults.Add(Expression.IfThen(test, appliedValidators));
                }
            }

            private static Expression ClearConverts(Expression exp)
            {
                while(exp.NodeType == ExpressionType.Convert)
                    exp = ((UnaryExpression)exp).Operand;
                return exp;
            }

            private static Expression FormatPaths(ExpressionPathsBuilder.SinglePaths paths)
            {
                var stringBuilder = new StringBuilder();
                var formattedPaths = new List<Expression>();
                foreach(var path in paths.paths)
                {
                    var arguments = new List<Expression>();
                    stringBuilder.Clear();
                    var first = true;
                    for(var i = 0; i < path.Count; i++)
                    {
                        var piece = ClearConverts(path[i]);
                        if(piece.Type == typeof(string))
                        {
                            // property or hashtable key
                            if(piece.NodeType != ExpressionType.Constant)
                                throw new InvalidOperationException("Expected constant");
                            if(!first)
                                stringBuilder.Append('.');
                            first = false;
                            stringBuilder.Append((string)((ConstantExpression)piece).Value);
                        }
                        else if(piece.Type == typeof(int))
                        {
                            // index
                            stringBuilder.Append("[{");
                            stringBuilder.Append(arguments.Count.ToString());
                            stringBuilder.Append("}]");
                            arguments.Add(Expression.Convert(piece, typeof(object)));
                        }
                    }
                    formattedPaths.Add(Expression.Call(stringFormatMethod,
                                                       Expression.Constant(stringBuilder.ToString()),
                                                       Expression.NewArrayInit(typeof(object), arguments)));
                }
                return Expression.MemberInit(
                    Expression.New(typeof(SimplePathFormatterText)),
                    Expression.Bind(pathsProperty, Expression.NewArrayInit(typeof(string), formattedPaths)));
            }

            private static Expression SelectTargetNode(ParameterExpression root, Type treeRootType, ResolvedArrayIndexes[] paths)
            {
                if(paths.Length == 0)
                    return root;
                var lcp = 0;
                for(;; ++lcp)
                {
                    var ok = true;
                    Expression ethalon = null;
                    foreach(var path in paths)
                    {
                        var pieces = path.path;
                        if(lcp >= pieces.Count)
                        {
                            ok = false;
                            break;
                        }
                        var piece = ClearConverts(pieces[lcp]);
                        if(ethalon == null)
                            ethalon = piece;
                        else
                        {
                            // expected either a constant or a parameter which is an index
                            if(ethalon.Type == typeof(int))
                            {
                                // index
                            }
                            else if(ethalon.Type == typeof(string))
                            {
                                // property or hashtable key
                                if(ethalon.NodeType != ExpressionType.Constant)
                                    throw new InvalidOperationException("Expected constant");
                                if(piece.NodeType != ExpressionType.Constant)
                                    throw new InvalidOperationException("Expected constant");
                                if((string)((ConstantExpression)ethalon).Value != (string)((ConstantExpression)piece).Value)
                                {
                                    ok = false;
                                    break;
                                }
                            }
                            else throw new InvalidOperationException(string.Format("Type '{0}' is not valid at this point", ethalon.Type));
                        }
                    }
                    if(!ok)
                        break;
                }
                --lcp;
                var retLabel = Expression.Label(typeof(ValidationResultTreeNode));
                var temps = new List<ParameterExpression>();
                var expressions = new List<Expression>();
                foreach(var path in paths)
                {
                    if(path.indexes != null)
                        temps.Add(path.indexes);
                    if(path.indexesInit != null)
                        expressions.Add(path.indexesInit);
                }
                var start = Expression.Parameter(treeRootType);
                temps.Add(start);
                Expression cur = start;
                expressions.Add(Expression.Assign(cur, Expression.Convert(root, treeRootType)));
                var curType = treeRootType;
                var insideHashtable = false;
                for(var i = 0; i <= lcp; ++i)
                {
                    var piece = ClearConverts(paths[0].path[i]);
                    if(insideHashtable)
                    {
                        var gotoChildMethod = HackHelpers.GetMethodDefinition<ValidationResultTreeUniversalNode>(x => x.GotoChild(null));
                        if(gotoChildMethod == null)
                            throw new InvalidOperationException("Method 'GotoChild' is not found");
                        cur = Expression.Call(Expression.Convert(cur, typeof(ValidationResultTreeUniversalNode)), gotoChildMethod, Expression.Call(piece, typeof(object).GetMethod("ToString", Type.EmptyTypes)));
                    }
                    else
                    {
                        if(piece.Type == typeof(string))
                        {
                            {
                                var fieldName = (string)((ConstantExpression)piece).Value;
                                var field = curType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
                                if(field == null)
                                    throw new InvalidOperationException(string.Format("Type '{0}' has no field '{1}'", curType, fieldName));
                                var next = Expression.Parameter(field.FieldType);
                                temps.Add(next);
                                var constructor = field.FieldType.GetConstructor(new[] {typeof(ValidationResultTreeNode)});
                                if(constructor == null)
                                    throw new InvalidOperationException(string.Format("The type '{0}' has no constructor accepting one parameter of type '{1}'", field.FieldType, typeof(ValidationResultTreeNode)));
                                expressions.Add(Expression.Assign(next, Expression.Field(cur, field)));
                                expressions.Add(Expression.IfThen(Expression.Equal(next, Expression.Constant(null, typeof(ValidationResultTreeNode))),
                                                                  Expression.Assign(Expression.Field(cur, field), Expression.Assign(next, Expression.New(constructor, cur)))));
                                cur = next;
                                curType = field.FieldType;
                                if(curType == typeof(ValidationResultTreeUniversalNode))
                                {
                                    insideHashtable = true;
                                }
                            }
                        }
                        else
                        {
                            // index
                            var curIndexes = new List<Expression>();
                            for(var j = 0; j < paths.Length; ++j)
                            {
                                curIndexes.Add(ClearConverts(paths[j].path[i]));
                            }
                            var elementType = curType.GetGenericArguments()[0];
                            var temp = Expression.Parameter(elementType);
                            temps.Add(temp);
                            var gotoChildMethod = curType.GetMethod("GotoChild", new[] {typeof(int[])});
                            if(gotoChildMethod == null)
                                throw new InvalidOperationException("Method 'GotoChild' is not found");
                            expressions.Add(Expression.Assign(temp, Expression.Convert(Expression.Call(cur, gotoChildMethod, Expression.NewArrayInit(typeof(int), curIndexes)), elementType)));
                            expressions.Add(Expression.IfThen(Expression.Equal(temp, Expression.Constant(null, typeof(ValidationResultTreeNode))), Expression.Return(retLabel, cur)));
                            cur = temp;
                            curType = elementType;
                        }
                    }
                }
                expressions.Add(Expression.Label(retLabel, cur));
                return Expression.Block(typeof(ValidationResultTreeNode), temps, expressions);
            }

            public readonly List<KeyValuePair<Expression, List<MutatorConfiguration>>> mutators = new List<KeyValuePair<Expression, List<MutatorConfiguration>>>();
            public readonly Dictionary<ExpressionWrapper, ZzzNode> children = new Dictionary<ExpressionWrapper, ZzzNode>();
            private static readonly MethodInfo selectMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, IEnumerable<int>>>)(enumerable => enumerable.Select(x => x))).Body).Method.GetGenericMethodDefinition();

            private static readonly MemberInfo pathsProperty = HackHelpers.GetProp<SimplePathFormatterText>(text => text.Paths);
            private static readonly MethodInfo stringFormatMethod = HackHelpers.GetMethodDefinition<object[]>(z => string.Format("zzz", z));

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

        public bool IsArrayIndex { get { return Value is int; } }
        public bool IsMemberAccess { get { return Value is PropertyInfo || Value is FieldInfo; } }
        public bool IsEachMethod { get { return ReferenceEquals(Value, MutatorsHelperFunctions.EachMethod); } }
        public bool IsConvertation { get { return Value is Type; } }
        public bool IsIndexerParams { get { return Value is object[]; } }

        public override int GetHashCode()
        {
            return GetHashCode(Value);
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
            return Compare(Value, other.Value);
        }

        public object Value { get; private set; }

        private static int GetHashCode(object value)
        {
            var arr = value as object[];
            if(arr == null)
            {
                if(value is int)
                    return (int)value;
                if(value is string)
                    return value.GetHashCode();
                var type = value as Type;
                if(type != null)
                    return type.GetHashCode();
                var memberInfo = (MemberInfo)value;
                unchecked
                {
                    return memberInfo.Module.MetadataToken * 397 + memberInfo.MetadataToken;
                }
            }
            var result = 0;
            foreach(var item in arr)
            {
                unchecked
                {
                    result = result * 397 + GetHashCode(item);
                }
            }
            return result;
        }

        private static bool Compare(object left, object right)
        {
            var leftAsArray = left as object[];
            var rightAsArray = right as object[];
            if(leftAsArray != null || rightAsArray != null)
            {
                if(leftAsArray == null || rightAsArray == null)
                    return false;
                if(leftAsArray.Length != rightAsArray.Length)
                    return false;
                return !leftAsArray.Where((t, i) => !Compare(t, rightAsArray[i])).Any();
            }
            if(left is int || right is int)
                return left is int && right is int && (int)left == (int)right;
            if(left is string || right is string)
                return left is string && right is string && (string)left == (string)right;
            if(left is Type || right is Type)
            {
                if(!(left is Type && right is Type))
                    return false;
                return (Type)left == (Type)right;
            }
            if(((MemberInfo)left).Module != ((MemberInfo)right).Module)
                return false;
            return ((MemberInfo)left).MetadataToken == ((MemberInfo)right).MetadataToken;
        }

        public static readonly PropertyInfo ArrayLengthProperty = (PropertyInfo)((MemberExpression)((Expression<Func<Array, int>>)(arr => arr.Length)).Body).Member;

        public static readonly ModelConfigurationEdge Each = new ModelConfigurationEdge(MutatorsHelperFunctions.EachMethod);
        public static readonly ModelConfigurationEdge ArrayLength = new ModelConfigurationEdge(ArrayLengthProperty);
    }
}