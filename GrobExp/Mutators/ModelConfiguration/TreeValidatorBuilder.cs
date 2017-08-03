using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using GrEmit.Utils;

using GrobExp.Mutators.Aggregators;
using GrobExp.Mutators.Exceptions;
using GrobExp.Mutators.MultiLanguages;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class TreeValidatorBuilder
    {
        public static LambdaExpression BuildTreeValidator(this ModelConfigurationNode node, IPathFormatter pathFormatter)
        {
            var allMutators = new Dictionary<ExpressionWrapper, List<MutatorConfiguration>>();
            node.GetMutators(allMutators);

            var root = new ZzzNode();
            foreach(var pair in allMutators)
            {
                var arrays = GetArrays(node.RootType, pair.Key.Expression, pair.Value);
                var arrayNode = arrays.Aggregate(root, (current, array) => current.Traverse(array));
                arrayNode.mutators.Add(new KeyValuePair<Expression, List<MutatorConfiguration>>(pair.Key.Expression, pair.Value));
            }

            var parameter = node.Parent == null ? (ParameterExpression)node.Path : Expression.Parameter(node.NodeType, node.NodeType.Name);
            var treeRootType = ValidationResultTreeNodeBuilder.BuildType(parameter.Type);
            var result = Expression.Parameter(typeof(ValidationResultTreeNode), "tree");
            var priority = Expression.Parameter(typeof(int), "priority");
            var aliases = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, node.Path)};

            root = GetArrays(node.RootType, node.Path, new MutatorConfiguration[0]).Aggregate(root, (current, array) => current.Traverse(array));

            var validationResults = new List<Expression>();
            root.BuildValidator(pathFormatter, node == node.Root ? null : node, aliases, new Dictionary<ParameterExpression, ExpressionPathsBuilder.SinglePaths>(), result, treeRootType, priority, validationResults);

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
            body = body.ExtractLoopInvariantFatExpressions(new[] {parameter}, expression => expression);
            var lambda = Expression.Lambda(body, parameter, result, priority);
            return lambda;
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

        private static void GetMutators(this ModelConfigurationNode node, Dictionary<ExpressionWrapper, List<MutatorConfiguration>> result)
        {
            if(node.mutators != null)
            {
                foreach(var pair in node.mutators)
                {
                    var key = new ExpressionWrapper(pair.Key, false);
                    List<MutatorConfiguration> list;
                    if(!result.TryGetValue(key, out list))
                        result.Add(key, list = new List<MutatorConfiguration>());
                    list.Add(pair.Value);
                }
            }
            foreach(var child in node.Children)
                child.GetMutators(result);
        }

        public const int PriorityShift = 1000;

        private static readonly ConstructorInfo formattedValidationResultConstructor = ((NewExpression)((Expression<Func<ValidationResult, FormattedValidationResult>>)(o => new FormattedValidationResult(o, null, null, 0))).Body).Constructor;

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
                            childValidationResults[i] = childValidationResults[i].ExtractLoopInvariantFatExpressions(aliases.Where(p => p.Key is ParameterExpression).Select(p => (ParameterExpression)p.Key), e => e);
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
}