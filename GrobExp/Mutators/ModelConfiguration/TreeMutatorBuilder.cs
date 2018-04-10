using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Compiler;
using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Exceptions;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class TreeMutatorBuilder
    {
        public static LambdaExpression BuildTreeMutator(this ModelConfigurationNode node)
        {
            return node.BuildTreeMutator(new List<ParameterExpression> {node.Parent == null ? (ParameterExpression)node.Path : Expression.Parameter(node.NodeType, node.NodeType.Name)});
        }

        public static LambdaExpression BuildTreeMutator(this ModelConfigurationNode node, Type type)
        {
            return node.BuildTreeMutator(new List<ParameterExpression>
                {
                    node.Parent == null ? (ParameterExpression)node.Path : Expression.Parameter(node.NodeType, node.NodeType.Name),
                    Expression.Parameter(type, type.Name)
                });
        }

        /// <summary>
        ///     Строит жирный Expression, содержаший все мутации или валидации.
        ///     Все, что нужно после этого - скомпилировать.
        /// </summary>
        /// <param name="parameters">Параметры Expression'а (для конвертации - target и source)</param>
        private static LambdaExpression BuildTreeMutator(this ModelConfigurationNode node, List<ParameterExpression> parameters)
        {
            var visitedNodes = new HashSet<ModelConfigurationNode>();
            var processedNodes = new HashSet<ModelConfigurationNode>();
            var mutatorExpressions = new List<Expression>();

            //Добавляем алиас для случая, когда билдим жиромутатор для поддерева, чтобы заменить путь до корня на реальный параметр выражения
            var aliases = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameters[0], node.Path)};
            var invariantParameters = new List<ParameterExpression>();
            if (parameters.Count > 1)
                invariantParameters.Add(parameters[1]);
            node.BuildTreeMutator(null, node, aliases, mutatorExpressions, visitedNodes, processedNodes, mutatorExpressions, invariantParameters);

            //Оптимизации
            mutatorExpressions = mutatorExpressions.SplitToBatches(parameters.ToArray());
            mutatorExpressions.Add(Expression.Empty());
            Expression body = Expression.Block(mutatorExpressions);
            body = LoopInvariantFatExpressionsExtractor.ExtractLoopInvariantFatExpressions(body, invariantParameters, expression => expression);

            //Далее параметры мутаторов подменяются на параметры итогового выражения
            foreach (var actualParameter in body.ExtractParameters())
            {
                var expectedParameter = parameters.Single(p => p.Type == actualParameter.Type);
                if (actualParameter != expectedParameter)
                    body = new ParameterReplacer(actualParameter, expectedParameter).Visit(body);
            }

            var result = Expression.Lambda(body, parameters);
            return result;
        }

        /// <summary>
        ///     Просто выполняет переход по ребру и строит выражения для перечисления массивов и словарей.
        /// </summary>
        private static void BuildTreeMutator(this ModelConfigurationNode node, ModelConfigurationEdge edge, Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult,
                                             HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            var child = node.children[edge];
            if (edge.IsMemberAccess || edge.IsArrayIndex || edge.IsConvertation || edge.IsIndexerParams)
                child.BuildTreeMutator(edges, root, aliases, localResult, visitedNodes, processedNodes, globalResult, invariantParameters);
            else if (edge.IsEachMethod)
            {
                var path = node.Path.ResolveAliases(aliases);
                if (node.NodeType.IsDictionary())
                    node.BuildTreeMutatorForDictionary(child, edges, root, aliases, localResult, visitedNodes, processedNodes, path, globalResult, invariantParameters);
                else
                    node.BuildTreeMutatorForArray(child, edges, root, aliases, localResult, visitedNodes, processedNodes, path, globalResult, invariantParameters);
            }
            else
                throw new InvalidOperationException();
        }

        /// <summary>
        ///     Разворачиваем всякие ичи-хуичи в нормальные циклы посредством хитровыебнутой функции MutatorsHelperFunctions.ForEach
        /// </summary>
        private static void BuildTreeMutatorForArray(this ModelConfigurationNode node, ModelConfigurationNode child, Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult, HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, Expression path, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
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
            var item = node.Path.MakeEachCall(child.NodeType);
            var index = item.MakeCurrentIndexCall(child.NodeType);
            aliases.Add(new KeyValuePair<Expression, Expression>(childParameter, item));
            aliases.Add(new KeyValuePair<Expression, Expression>(indexParameter, index));

            // Find a source array to take values from
            // todo ich: почему только первый?
            var arrays = node.GetArrays();
            var canonizedFullPath = new MethodReplacer(MutatorsHelperFunctions.EachMethod, MutatorsHelperFunctions.CurrentMethod).Visit(node.Path);
            var array = arrays.FirstOrDefault(pair => !new ExpressionWrapper(pair.Value, false).Equals(new ExpressionWrapper(canonizedFullPath, false))).Value;

            ParameterExpression arrayParameter = null;
            var itemType = array == null ? null : array.Type.GetItemType();
            if (array != null)
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
            if (array != null)
            {
                invariantParameters.RemoveAt(invariantParameters.Count - 1);
                aliases.RemoveAt(aliases.Count - 1);
                aliases.RemoveAt(aliases.Count - 1);
            }

            if (childResult.Count > 0)
            {
                // ForEach method requires mutators block to return the target item
                childResult.Add(childParameter);

                // Optimization of block size to avoid big functions
                var action = Expression.Block(childResult.SplitToBatches());
                // Make a call to MutatorsHelperFunctions.ForEach
                var forEach = action.ExtractLoopInvariantFatExpressions(invariantParameters, exp => Expression.Call(null, forEachMethod.MakeGenericMethod(child.NodeType), new[] {path, Expression.Lambda(exp, childParameter, indexParameter)}));
                Expression result;
                if (array == null)
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

            if (array != null)
            {
                invariantParameters.RemoveAt(invariantParameters.Count - 1);
                invariantParameters.RemoveAt(invariantParameters.Count - 1);
            }
        }

        private static Expression CreateOrResizeArrayIfNeeded(ModelConfigurationNode child, Expression path, ParameterExpression arrayParameter)
        {
            Expression destArrayIsNull = Expression.ReferenceEqual(path, Expression.Constant(null, path.Type));
            Expression resizeIfNeeded;
            if (path.Type.IsArray)
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
            else if (path.Type.IsGenericType && path.Type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Expression lengthsAreDifferent = Expression.NotEqual(Expression.Property(path, "Count"), Expression.ArrayLength(arrayParameter));
                var expressions = new List<Expression>();
                if (path.NodeType == ExpressionType.MemberAccess && CanWrite(((MemberExpression)path).Member))
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
        private static void BuildTreeMutatorForDictionary(this ModelConfigurationNode node, ModelConfigurationNode child, Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult, HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, Expression path, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            // Единственное отличие от BuildTreeMutatorForArray, состоит в том, что нужно отдельно найти мутатор для ключа, чтобы запихать его в ForEachOverDictionary
            var arguments = child.NodeType.GetGenericArguments();
            var destKeyType = arguments[0];
            var destValueType = arguments[1];
            var destValueParameter = Expression.Variable(destValueType);
            var destKeyParameter = Expression.Variable(destKeyType);
            var destArrayEach = Expression.Call(null, MutatorsHelperFunctions.EachMethod.MakeGenericMethod(child.NodeType), node.Path);
            var destValue = Expression.Property(destArrayEach, "Value");
            var destKey = Expression.Property(destArrayEach, "Key");

            aliases.Add(new KeyValuePair<Expression, Expression>(destValueParameter, destValue));
            aliases.Add(new KeyValuePair<Expression, Expression>(destKeyParameter, destKey));

            // todo ich: почему только первый?
            var array = node.GetArrays().Single().Value;
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
            if (childResult.Count > 0)
            {
                var indexOfKeyAssigner = -1;
                for (var i = 0; i < childResult.Count; ++i)
                {
                    if (childResult[i].NodeType == ExpressionType.Assign && ((BinaryExpression)childResult[i]).Left == destKeyParameter)
                    {
                        indexOfKeyAssigner = i;
                        break;
                    }
                }

                if (indexOfKeyAssigner < 0)
                    throw new InvalidOperationException("Key selector is missing");
                var keySelector = Expression.Lambda(((BinaryExpression)childResult[indexOfKeyAssigner]).Right, sourceKeyParameter);
                childResult.RemoveAt(indexOfKeyAssigner);

                childResult.Add(destValueParameter);
                var action = Expression.Block(childResult.SplitToBatches());
                var forEach = LoopInvariantFatExpressionsExtractor.ExtractLoopInvariantFatExpressions(action, invariantParameters, exp => Expression.Call(null, forEachOverDictionaryMethod.MakeGenericMethod(sourceKeyType, destKeyType, sourceValueType, destValueType), array, path, keySelector, Expression.Lambda(exp, sourceValueParameter, destValueParameter)));

                var extendedDict = CreateDictIfNull(path);
                localResult.Add(Expression.Block(new[] {extendedDict, forEach}));
            }
        }

        private static Expression CreateDictIfNull(Expression path)
        {
            if (path.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = (MemberExpression)path;
                var lazyType = typeof(Lazy<>).MakeGenericType(path.Type);
                if (memberExpression.Member == lazyType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public))
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
        /// <param name="invariantParameters">Список параметров, использующихся в <see cref="LoopInvariantFatExpressionsExtractor.ExtractLoopInvariantFatExpressions">ExtractLoopInvariantFatExpressions</see> для оптимизации циклов. Эти параметры - синонимы путей от source</param>
        private static void BuildTreeMutator(this ModelConfigurationNode node, Stack<ModelConfigurationEdge> edges, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases, List<Expression> localResult,
                                             HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            // Если у нас есть ещё рёбра - переходим по ним, 'телепортируясь' к ноде с нужной нам зависимостью.
            if (edges != null && edges.Count != 0)
                node.BuildTreeMutator(edges.Pop(), edges, root, aliases, localResult, visitedNodes, processedNodes, globalResult, invariantParameters);
            else
            {
                node.BuildNodeMutator(root, aliases, localResult, visitedNodes, processedNodes, globalResult, invariantParameters);
                foreach (var entry in node.children)
                    node.BuildTreeMutator(entry.Key, edges, root, aliases, localResult, visitedNodes, processedNodes, globalResult, invariantParameters);
            }
        }

        private static void BuildNodeMutator(this ModelConfigurationNode node, ModelConfigurationNode root, List<KeyValuePair<Expression, Expression>> aliases,
                                             List<Expression> localResult, HashSet<ModelConfigurationNode> visitedNodes, HashSet<ModelConfigurationNode> processedNodes, List<Expression> globalResult, List<ParameterExpression> invariantParameters)
        {
            var path = node.Path.ResolveAliases(aliases);
            if (visitedNodes.Contains(node))
            {
                if (!processedNodes.Contains(node))
                    throw new FoundCyclicDependencyException("A cycle encountered started at '" + node.Path + "'");
                return;
            }

            visitedNodes.Add(node);

            var selfDependentMutators = new List<AutoEvaluatorConfiguration>();
            var otherMutators = new List<AutoEvaluatorConfiguration>();
            foreach (var mutator in node.Mutators.Where(mutator => mutator.Value is AutoEvaluatorConfiguration))
            {
                var selfDependent = false;
                foreach (var dependency in mutator.Value.Dependencies ?? new LambdaExpression[0])
                {
                    ModelConfigurationNode child;
                    // Спускаемся из глобального корня по найденной зависимости.
                    // Важно, чтобы путь проходил через корень поддерва, для которого мы изначально запустили BuildTreeMutator.
                    // Иначе есть какая-то внешняя зависимость, и ничего работать не будет.
                    if (!node.Root.Traverse(dependency.Body, root, out child, false))
                        throw new FoundExternalDependencyException("Unable to build mutator for the subtree '" + node.Path + "' due to the external dependency '" + dependency + "'");

                    if (child == null)
                    {
                        // Если у нас в дереве нет вершины соответствующей пути до зависимости, т.е. нет конфигураций для её заполнения,
                        // нужно найти самый длинный префикс этого пути, в котором такая конфигурация есть.
                        var found = false;
                        var shards = dependency.Body.SmashToSmithereens();
                        for (var i = shards.Length - 1; i >= 0; --i)
                        {
                            node.Root.Traverse(shards[i], root, out child, false);
                            if (child != null && child.Mutators.Any(pair => pair.Value is EqualsToConfiguration))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found) child = null;
                    }

                    if (child != null && child != node)
                    {
                        // Если зависимости соответствует какая-то вершина в дереве (кроме корня), то надо сначала построить выражения 
                        // для её поддерева, которые должны идти перед выражениями для текущей вершины. 
                        // Потом нужно к ней аккуратно спуститься от корня, не вызывая билд для промежуточных вершин.
                        // Для этого, запоминаем путь до неё в стек рёбер.
                        var edges = new Stack<ModelConfigurationEdge>();
                        var upNode = child;
                        while (upNode != root)
                        {
                            edges.Push(upNode.Edge);
                            upNode = upNode.Parent;
                        }

                        // Запускаем билд для зависимости, безо всех текущих алиасов на массивы.
                        root.BuildTreeMutator(edges, root, new List<KeyValuePair<Expression, Expression>> {aliases.First()}, globalResult, visitedNodes, processedNodes, globalResult, invariantParameters);
                    }

                    selfDependent |= child == node;
                }

                (selfDependent ? selfDependentMutators : otherMutators).Add((AutoEvaluatorConfiguration)mutator.Value);
            }

            // Добавляем все выражения мутаторов к результату.
            // При этом важно, чтобы выражения мутаторов, которые зависят от себя, шли после всех остальных. 
            // Например поле заполняется, а потом, при определённом условии реконфигурируется.
            localResult.AddRange(otherMutators.Concat(selfDependentMutators)
                                              .Select(mutator => mutator.Apply(path, aliases).EliminateLinq())
                                              .Where(expression => expression != null));
            processedNodes.Add(node);
        }

        private static void Resize<T>(List<T> list, int size)
        {
            // todo emit
            if (list.Count > size)
            {
                while (list.Count > size)
                    list.RemoveAt(list.Count - 1);
            }
            else
            {
                while (list.Count < size)
                    list.Add(default(T));
            }
        }

        private static readonly MethodInfo forEachMethod = ((MethodCallExpression)((Expression<Action<bool[]>>)(arr => MutatorsHelperFunctions.ForEach(arr, null))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo forEachReadonlyMethod = ((MethodCallExpression)((Expression<Action<IEnumerable<int>>>)(enumerable => MutatorsHelperFunctions.ForEach(enumerable, null))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo forEachOverDictionaryMethod = ((MethodCallExpression)((Expression<Action<Dictionary<int, int>, Dictionary<int, int>>>)((source, dest) => MutatorsHelperFunctions.ForEach(source, dest, i => i, (x, y) => x + y))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo toArrayMethod = ((MethodCallExpression)((Expression<Func<int[], int[]>>)(ints => ints.ToArray())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo arrayResizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 5))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo listResizeMethod = ((MethodCallExpression)((Expression<Action<List<int>>>)(list => Resize(list, 5))).Body).Method.GetGenericMethodDefinition();
    }
}