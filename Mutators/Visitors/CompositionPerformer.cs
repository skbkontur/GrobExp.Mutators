using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.ModelConfiguration;
using GrobExp.Mutators.Validators;
using GrobExp.Mutators.Visitors.CompositionPerforming;

using JetBrains.Annotations;

namespace GrobExp.Mutators.Visitors
{
    public class CompositionPerformer : ExpressionVisitor
    {
        public CompositionPerformer(Type from, Type to, ModelConfigurationNode convertationTree)
        {
            From = from;
            To = to;
            this.convertationTree = convertationTree;
            var parameter = Expression.Parameter(to);
            lambda = Expression.Lambda(parameter, parameter);
        }

        public Expression Perform(Expression expression)
        {
            resolved = true;
            var result = Visit(expression);
            return resolved ? result : null;
        }

        public List<KeyValuePair<Expression, Expression>> GetConditionalSetters([CanBeNull] Expression node)
        {
            if (node == null || !IsSimpleLinkOfChain(node, out var type))
                return null;
            if (type != From) return null;

            node = node.CleanFilters(out var filters);

            var shards = node.SmashToSmithereens();
            for (var i = shards.Length - 1; i >= 0; --i)
            {
                var conditionalSetters = GetConditionalSettersInternal(shards[i], out var onlyLeavesAreConvertible);
                if (onlyLeavesAreConvertible && (i < shards.Length - 1 || !shards[i].Type.IsArray))
                    return null;
                if (conditionalSetters == null)
                    continue;
                if (i == shards.Length - 1)
                    return conditionalSetters.Select(item => new KeyValuePair<Expression, Expression>(ApplyFilters(item.Key, filters), item.Value)).ToList();
                if (conditionalSetters.Count == 1 && conditionalSetters[0].Value == null)
                    return new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(ApplyFilters(Merge(conditionalSetters[0].Key, shards.Skip(i + 1)), filters), null)};
                return conditionalSetters.Select(item => new KeyValuePair<Expression, Expression>(ApplyFilters(Merge(item.Key, shards.Skip(i + 1)), filters), item.Value)).ToList();
            }

            return null;
        }

        public override Expression Visit(Expression node)
        {
            if (IsSimpleLinkOfChain(node, out var type))
                return type == From ? ResolveChain(node) : node;
            return base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member == stringLengthProperty)
            {
                var expression = Visit(node.Expression);
                if (expression.Type != typeof(string))
                    expression = Expression.Convert(expression, typeof(string));
                return node.Update(expression);
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);
            if (left.Type != node.Left.Type)
                left = Expression.Convert(left, node.Left.Type);
            if (right.Type != node.Right.Type)
                right = Expression.Convert(right, node.Right.Type);
            return node.Update(left, (LambdaExpression)Visit(node.Conversion), right);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if (method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();
            if (method == MutatorsHelperFunctions.CurrentIndexMethod)
            {
                var item = (MethodCallExpression)node.Arguments.Single();
                if (!item.Method.IsEachMethod() && !item.Method.IsCurrentMethod())
                    throw new InvalidOperationException();
                var collection = Visit(item.Arguments.Single());
                var itemType = collection.Type.GetItemType();
                return Expression.Call(method.MakeGenericMethod(itemType), Expression.Call(item.Method.GetGenericMethodDefinition().MakeGenericMethod(itemType), collection));
            }

            if (method.DeclaringType != typeof(Enumerable))
                return base.VisitMethodCall(node);
            var obj = node.Arguments[0];
            var arguments = node.Arguments.Skip(1).ToArray();
            var visitedObj = Visit(obj);
            if (obj.Type == visitedObj.Type)
                return node.Update(node.Object, new[] {visitedObj}.Concat(arguments.Select(Visit)));
            var visitedArguments = new List<Expression>();
            var path = Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(obj.Type.GetItemType()), obj);
            foreach (var argument in arguments)
            {
                if (!(argument is LambdaExpression))
                    visitedArguments.Add(Visit(argument));
                else
                {
                    var lambdaArg = (LambdaExpression)argument;
                    if (lambdaArg.Parameters.Count != 1)
                        throw new NotSupportedException("Unsupported lambda " + ExpressionCompiler.DebugViewGetter(lambdaArg));
                    lambdaArg = Expression.Lambda(path, path.ExtractParameters()).Merge(lambdaArg);
                    var visitedArg = Visit(lambdaArg.Body);
                    var parameter = Expression.Parameter(visitedObj.Type.GetItemType());
                    var resolvedArg = new AliasesResolver(new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(visitedObj.Type.GetItemType()), visitedObj))}).Visit(visitedArg);
                    visitedArguments.Add(Expression.Lambda(resolvedArg, parameter));
                }
            }

            return Expression.Call(method.MakeGenericMethod(new[] {visitedObj.Type.GetItemType()}.Concat(node.Method.GetGenericArguments().Skip(1)).ToArray()), new[] {visitedObj}.Concat(visitedArguments));
        }

        private Expression ApplyFilters(Expression node, LambdaExpression[] filters)
        {
            if (filters.All(exp => exp == null))
                return node;
            var shards = node.SmashToSmithereens();
            var result = shards[0];
            int index = 0;
            for (int i = 1; i < shards.Length; ++i)
            {
                var shard = shards[i];
                switch (shard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    result = Expression.MakeMemberAccess(result, ((MemberExpression)shard).Member);
                    break;
                case ExpressionType.ArrayIndex:
                    result = Expression.ArrayIndex(result, ((BinaryExpression)shard).Right);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)shard;
                    if (methodCallExpression.Method.IsCurrentMethod() || methodCallExpression.Method.IsEachMethod())
                    {
                        var filter = filters[index++];
                        if (filter != null)
                        {
                            var performedFilter = Perform(filter.Body);
                            var parameter = Expression.Parameter(result.Type.GetItemType());
                            var aliasez = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, methodCallExpression)};
                            var resolvedPerformedFilter = new AliasesResolver(aliasez).Visit(performedFilter);
                            result = Expression.Call(whereMethod.MakeGenericMethod(result.Type.GetItemType()), result, Expression.Lambda(resolvedPerformedFilter, parameter));
                            result = Expression.Call(methodCallExpression.Method, result);
                            break;
                        }
                    }

                    result = methodCallExpression.Method.IsStatic
                                 ? Expression.Call(methodCallExpression.Method, new[] {result}.Concat(methodCallExpression.Arguments.Skip(1)))
                                 : Expression.Call(result, methodCallExpression.Method, methodCallExpression.Arguments);
                    break;
                default:
                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
                }
            }

            if (index < filters.Length)
            {
                var filter = filters[index++];
                var performedFilter = Perform(filter.Body);
                var parameter = Expression.Parameter(result.Type.GetItemType());
                var aliasez = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(node.Type.GetItemType()), node))};
                var resolvedPerformedFilter = new AliasesResolver(aliasez).Visit(performedFilter);
                result = Expression.Call(whereMethod.MakeGenericMethod(result.Type.GetItemType()), result, Expression.Lambda(resolvedPerformedFilter, parameter));
            }

            if (index < filters.Length)
                throw new InvalidOperationException("Too many filters to apply");
            return result;
        }

        private Expression Convert(Expression operand, Type type)
        {
            return operand.Type == type ? operand : Expression.Convert(operand, type);
        }

        private Type GetMemberType(MemberInfo member)
        {
            switch (member.MemberType)
            {
            case MemberTypes.Field:
                return ((FieldInfo)member).FieldType;
            case MemberTypes.Property:
                return ((PropertyInfo)member).PropertyType;
            default:
                throw new NotSupportedException("Member type '" + member.MemberType + "' is not supported");
            }
        }

        private Expression Construct(Type type, Hashtable node)
        {
            if (!type.IsArray)
            {
                return Expression.MemberInit(Expression.New(type),
                                             (from DictionaryEntry entry in node
                                              let member = (MemberInfo)entry.Key
                                              select Expression.Bind(member, entry.Value is Hashtable
                                                                                 ? Construct(GetMemberType(member), (Hashtable)entry.Value)
                                                                                 : Expression.Convert((Expression)entry.Value, GetMemberType(member)))).Cast<MemberBinding>().ToList());
            }

            var maxIndex = node.Keys.Cast<int>().Max();
            var elementType = type.GetElementType();
            return Expression.NewArrayInit(elementType, new int[maxIndex + 1].Select((x, i) =>
                {
                    var item = node[i];
                    return item == null ? Expression.Default(elementType) : Construct(elementType, (Hashtable)item);
                }));
        }

        private Expression ConstructByLeaves(Expression path, IEnumerable<KeyValuePair<Expression, Expression>> leaves)
        {
            ParameterExpression parameter = Expression.Parameter(path.Type);
            var resolver = new AliasesResolver(new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, path)});
            var tree = new Hashtable();
            foreach (var leaf in leaves)
            {
                var pathToLeaf = resolver.Visit(leaf.Key);
                var shards = pathToLeaf.SmashToSmithereens();
                var node = tree;
                for (int i = 1; i < shards.Length; ++i)
                {
                    var shard = shards[i];
                    object key;
                    switch (shard.NodeType)
                    {
                    case ExpressionType.MemberAccess:
                        key = ((MemberExpression)shard).Member;
                        break;
                    case ExpressionType.ArrayIndex:
                        var index = ((BinaryExpression)shard).Right;
                        if (index.NodeType != ExpressionType.Constant)
                            throw new NotSupportedException("Node type '" + index.NodeType + "' is not supported");
                        key = ((ConstantExpression)index).Value;
                        break;
                    default:
                        return null;
                    }

                    if (i == shards.Length - 1)
                        node[key] = leaf.Value;
                    else
                    {
                        if (node[key] == null)
                            node[key] = new Hashtable();
                        node = (Hashtable)node[key];
                    }
                }
            }

            return Construct(path.Type, tree);
        }

        private List<KeyValuePair<Expression, Expression>> GetConditionalSettersInternal(Expression node, out bool onlyLeavesAreConvertible)
        {
            onlyLeavesAreConvertible = false;
            var convertationNode = convertationTree.Traverse(node, false, out var arrayAliases);
            if (convertationNode == null)
                return null;
            var setters = convertationNode.GetMutators().OfType<EqualsToConfiguration>().ToArray();
            if (setters.Length == 0)
            {
                onlyLeavesAreConvertible = true;
                return GetConditionalSettersByLeaves(node, convertationNode);
            }
            var resolver = new AliasesResolver(arrayAliases);

            var result = new List<KeyValuePair<Expression, Expression>>();
            var wasUnconditionalSetter = false;
            for (var index = setters.Length - 1; index >= 0; --index)
            {
                var mutator = setters[index];
                LambdaExpression value;
                Expression condition;
                StaticValidatorConfiguration validator;
                var equalsToIfConfiguration = mutator as EqualsToIfConfiguration;
                if (equalsToIfConfiguration == null)
                {
                    if (wasUnconditionalSetter)
                        continue;
                    wasUnconditionalSetter = true;
                    value = mutator.Value;
                    condition = null;
                    validator = mutator.Validator;
                }
                else
                {
                    value = equalsToIfConfiguration.Value;
                    condition = lambda.Merge(Perform(equalsToIfConfiguration.Condition)).Body;
                    validator = equalsToIfConfiguration.Validator;
                }

                if (validator != null)
                {
                    if (arrayAliases != null)
                    {
                        var validationResult = validator.Apply(mutator.ConverterType, arrayAliases);
                        if (validationResult != null)
                        {
                            validationResult = Expression.Coalesce(validationResult, Expression.Constant(ValidationResult.Ok));
                            var valueIsValid = Expression.NotEqual(Expression.MakeMemberAccess(validationResult, validationResultTypeProperty), Expression.Constant(ValidationResultType.Error));
                            condition = condition == null ? valueIsValid : Expression.AndAlso(Convert(condition, typeof(bool)), valueIsValid).CanonizeParameters();
                        }
                    }
                }

                result.Add(new KeyValuePair<Expression, Expression>(resolver.Visit(lambda.Merge(Perform(value)).Body), resolver.Visit(condition)));
            }

            return result;
        }

        [CanBeNull]
        private List<KeyValuePair<Expression, Expression>> GetConditionalSettersByLeaves(Expression node, [NotNull] ModelConfigurationNode convertationNode)
        {
            if (node.Type.IsArray)
            {
                var arrays = convertationNode.GetArrays();
                if (arrays.TryGetValue(To, out var array) && array != null)
                {
                    var arrayItemConvertationNode = convertationNode.GotoEachArrayElement(false);
                    if (arrayItemConvertationNode != null)
                    {
                        var setter = (EqualsToConfiguration)arrayItemConvertationNode.GetMutators().SingleOrDefault(mutator => mutator is EqualsToConfiguration);
                        if (setter != null)
                        {
                            var convertedArray = ConvertArray(array, setter.Value.Body);
                            return new List<KeyValuePair<Expression, Expression>> { new KeyValuePair<Expression, Expression>(convertedArray, null) };
                        }
                    }

                    return new List<KeyValuePair<Expression, Expression>> { new KeyValuePair<Expression, Expression>(array, null) };
                }
            }

            var children = new List<ModelConfigurationNode>();
            convertationNode.FindSubNodes(children);
            children = children.Where(child => child.GetMutators().Any(mutator => mutator is EqualsToConfiguration)).ToList();
            if (children.Count > 0)
            {
                var leaves = new List<KeyValuePair<Expression, Expression>>();
                foreach (var child in children)
                {
                    var leaf = Perform(child.Path);
                    if (leaf != null)
                        leaves.Add(new KeyValuePair<Expression, Expression>(child.Path, leaf));
                }

                var constructedByLeaves = ConstructByLeaves(node, leaves);
                if (constructedByLeaves == null) return null;
                return new List<KeyValuePair<Expression, Expression>> { new KeyValuePair<Expression, Expression>(constructedByLeaves, null) };
            }

            return null;
        }

        private static Expression ConvertArray(Expression array, Expression expression)
        {
            var itemType = array.Type.GetItemType();
            var parameter = Expression.Parameter(itemType);
            expression = expression.ResolveAliases(new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(itemType), array))});
            if (expression.NodeType == ExpressionType.Parameter)
                return array;
            Expression select = Expression.Call(selectMethod.MakeGenericMethod(itemType, expression.Type), array, Expression.Lambda(expression, parameter));
            return Expression.Call(toArrayMethod.MakeGenericMethod(expression.Type), select);
        }

        private LambdaExpression Perform(LambdaExpression lambdaExpression)
        {
            var body = Visit(lambdaExpression.Body).CanonizeParameters();
            return Expression.Lambda(body, body.ExtractParameters());
        }

        private Expression Merge(Expression exp, IEnumerable<Expression> shards)
        {
            foreach (var shard in shards)
            {
                switch (shard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    exp = Expression.MakeMemberAccess(exp, ((MemberExpression)shard).Member);
                    break;
                case ExpressionType.ArrayIndex:
                    exp = Expression.ArrayIndex(exp, ((BinaryExpression)shard).Right);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)shard;
                    if (methodCallExpression.Method.IsCurrentMethod() || methodCallExpression.Method.IsEachMethod() || methodCallExpression.Method.IsTemplateIndexMethod())
                        exp = Expression.Call(methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(exp.Type.GetItemType()), exp);
                    else if (methodCallExpression.Method.IsIndexerGetter())
                        exp = Expression.Call(exp, exp.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), methodCallExpression.Arguments);
                    else throw new NotSupportedException("Method '" + methodCallExpression.Method + "' is not supported");
                    break;
                default:
                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
                }
            }

            return exp;
        }

        private static Expression ConvertToNullable(Expression expression, Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Expression.Convert(expression, type);
            return expression;
        }

        private Expression ResolveChain(Expression node)
        {
            var conditionalSetters = GetConditionalSetters(node);
            if (conditionalSetters == null)
                return Expression.Constant(node.Type.GetDefaultValue(), node.Type);
            var unconditionalSetter = conditionalSetters.SingleOrDefault(pair => pair.Value == null);
            Expression result = ConvertToNullable(unconditionalSetter.Key ?? Expression.Constant(node.Type.GetDefaultValue(), node.Type), node.Type);
            foreach (var item in conditionalSetters)
            {
                var condition = item.Value;
                var value = ConvertToNullable(item.Key, node.Type);
                if (condition == null)
                    continue;
                var test = Convert(condition, typeof(bool));
                result = Expression.Condition(test, value, result);
            }

            return result;
        }

        private static bool IsSimpleLinkOfChain([CanBeNull] Expression node, [CanBeNull] out Type type)
        {
            return IsSimpleLinkOfChainChecker.IsSimpleLinkOfChain(node, out type);
        }

        private Type From { get; }
        private Type To { get; }

        private readonly LambdaExpression lambda;

        private bool resolved;
        private readonly ModelConfigurationNode convertationTree;

        // ReSharper disable StaticFieldInGenericType
        private static readonly MethodInfo selectMethod = ((MethodCallExpression)((Expression<Func<int[], IEnumerable<int>>>)(ints => ints.Select(i => i + 1))).Body).Method.GetGenericMethodDefinition();
        private static readonly MemberInfo stringLengthProperty = ((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member;
        private static readonly MethodInfo toArrayMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, int[]>>)(enumerable => enumerable.ToArray())).Body).Method.GetGenericMethodDefinition();
        private static readonly MemberInfo validationResultTypeProperty = ((MemberExpression)((Expression<Func<ValidationResult, ValidationResultType>>)(result => result.Type)).Body).Member;

        private static readonly MethodInfo whereMethod = ((MethodCallExpression)((Expression<Func<int[], IEnumerable<int>>>)(ints => ints.Where(x => x == 0))).Body).Method.GetGenericMethodDefinition();
        // ReSharper restore StaticFieldInGenericType
    }
}