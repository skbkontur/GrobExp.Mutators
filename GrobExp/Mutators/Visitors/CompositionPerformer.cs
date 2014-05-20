using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.AutoEvaluators;
using GrobExp.Mutators.Validators;

namespace GrobExp.Mutators.Visitors
{
    public class CompositionPerformer : ExpressionVisitor
    {
        public CompositionPerformer(Type from, Type to, ModelConfigurationNode convertationTree, List<KeyValuePair<Expression, Expression>> aliases)
        {
            From = from;
            To = to;
            this.convertationTree = convertationTree;
            this.aliases = aliases;
            var parameter = Expression.Parameter(to);
            lambda = Expression.Lambda(parameter, parameter);
        }

        public Expression Perform(Expression expression)
        {
            resolved = true;
            var result = Visit(expression);
            return resolved ? result : null;
        }

        private static Expression CleanFilters(Expression node, List<LambdaExpression> filters)
        {
            var shards = node.SmashToSmithereens();
            Expression result = shards[0];
            int i = 0;
            while(i + 1 < shards.Length)
            {
                ++i;
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
                    if(methodCallExpression.Method.IsWhereMethod() && i + 1 < shards.Length && shards[i + 1].NodeType == ExpressionType.Call && (((MethodCallExpression)shards[i + 1]).Method.IsCurrentMethod() || ((MethodCallExpression)shards[i + 1]).Method.IsEachMethod()))
                    {
                        result = Expression.Call(((MethodCallExpression)shards[i + 1]).Method, result);
                        filters.Add(Expression.Lambda(result, (ParameterExpression)shards[0]).Merge((LambdaExpression)methodCallExpression.Arguments[1]));
                        ++i;
                    }
                    else
                    {
                        if(methodCallExpression.Method.IsCurrentMethod() || methodCallExpression.Method.IsEachMethod())
                            filters.Add(null);
                        result = methodCallExpression.Method.IsStatic
                                     ? Expression.Call(methodCallExpression.Method, new[] {result}.Concat(methodCallExpression.Arguments.Skip(1)))
                                     : Expression.Call(result, methodCallExpression.Method, methodCallExpression.Arguments);
                    }
                    break;
                default:
                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
                }
            }
            return result;
        }

        private Expression ApplyFilters(Expression node, List<LambdaExpression> filters)
        {
            if(filters.All(exp => exp == null))
                return node;
            var shards = node.SmashToSmithereens();
            var result = shards[0];
            int index = 0;
            for(int i = 1; i < shards.Length; ++i)
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
                    if(methodCallExpression.Method.IsCurrentMethod() || methodCallExpression.Method.IsEachMethod())
                    {
                        var filter = filters[index++];
                        if(filter != null)
                        {
//                            var performedShard = Perform(shard);
                            var performedFilter = Perform(filter.Body);
                            var parameter = Expression.Parameter(result.Type.GetItemType());
                            var aliasez = new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, methodCallExpression)};
                            var resolvedPerformedFilter = new AliasesResolver(aliasez, false).Visit(performedFilter);
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
            return result;
        }

        public List<KeyValuePair<Expression, Expression>> GetConditionalSetters(Expression node)
        {
            Type type;
            if(!IsSimpleLinkOfChain(node, out type))
                return null;
            if(type != From) return null;

            var filters = new List<LambdaExpression>();
            node = CleanFilters(node, filters);

            var shards = node.SmashToSmithereens();
            for(var i = shards.Length - 1; i >= 0; --i)
            {
                var conditionalSetters = GetConditionalSettersInternal(shards[i]);
                if(conditionalSetters == null)
                    continue;
                if(i == shards.Length - 1)
                    return conditionalSetters.Select(item => new KeyValuePair<Expression, Expression>(ApplyFilters(item.Key, filters), item.Value)).ToList();
                if(conditionalSetters.Count == 1 && conditionalSetters[0].Value == null)
                {
                    var key = conditionalSetters[0].Key;
                    if(key.Type != shards[i].Type)
                        continue;
                    return new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(ApplyFilters(Merge(key, shards.Skip(i + 1)), filters), null)};
                }
                return conditionalSetters.Select(item => new KeyValuePair<Expression, Expression>(ApplyFilters(Merge(item.Key, shards.Skip(i + 1)), filters), item.Value)).ToList();
            }
            return null;
        }

        public override Expression Visit(Expression node)
        {
            Type type;
            if(IsSimpleLinkOfChain(node, out type))
                return type == From ? ResolveChain(node) : node;
            return base.Visit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if(method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();
            if(method == MutatorsHelperFunctions.CurrentIndexMethod)
            {
                var item = (MethodCallExpression)node.Arguments.Single();
                if(!item.Method.IsEachMethod() && !item.Method.IsCurrentMethod())
                    throw new InvalidOperationException();
                var collection = Visit(item.Arguments.Single());
                var itemType = collection.Type.GetItemType();
                return Expression.Call(method.MakeGenericMethod(itemType), Expression.Call(item.Method.GetGenericMethodDefinition().MakeGenericMethod(itemType), collection));
            }
            if(method.DeclaringType != typeof(Enumerable))
                return base.VisitMethodCall(node);
            var obj = node.Arguments[0];
            var arguments = node.Arguments.Skip(1).ToArray();
            var visitedObj = Visit(obj);
            if(obj.Type == visitedObj.Type)
                return node.Update(node.Object, new[] {visitedObj}.Concat(arguments.Select(Visit)));
            var visitedArguments = new List<Expression>();
            var path = Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(obj.Type.GetItemType()), obj);
            foreach(var argument in arguments)
            {
                if(!(argument is LambdaExpression))
                    visitedArguments.Add(Visit(argument));
                else
                {
                    var lambdaArg = (LambdaExpression)argument;
                    if(lambdaArg.Parameters.Count != 1)
                        throw new NotSupportedException("Unsupported lambda " + ExpressionCompiler.DebugViewGetter(lambdaArg));
                    lambdaArg = Expression.Lambda(path, path.ExtractParameters()).Merge(lambdaArg);
                    var visitedArg = Visit(lambdaArg.Body);
                    var parameter = Expression.Parameter(visitedObj.Type.GetItemType());
                    var resolvedArg = new AliasesResolver(new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(visitedObj.Type.GetItemType()), visitedObj))}, false).Visit(visitedArg);
                    visitedArguments.Add(Expression.Lambda(resolvedArg, parameter));
                }
            }
            return Expression.Call(method.MakeGenericMethod(new[] {visitedObj.Type.GetItemType()}.Concat(node.Method.GetGenericArguments().Skip(1)).ToArray()), new[] {visitedObj}.Concat(visitedArguments));
        }

        private Expression Convert(Expression operand, Type type)
        {
            return operand.Type == type ? operand : Expression.Convert(operand, type);
        }

        private List<KeyValuePair<Expression, Expression>> GetConditionalSettersInternal(Expression node)
        {
            var convertationNode = convertationTree.Traverse(node, false);
            if(convertationNode == null)
                return null;
            var setters = convertationNode.GetMutators().Where(mutator => mutator is EqualsToConfiguration).ToArray();
            if(setters.Length == 0)
            {
                if(node.Type.IsArray)
                {
                    var arrays = convertationNode.GetArrays(true);
                    Expression array;
                    if(arrays.TryGetValue(To, out array) && array != null)
                    {
                        var arrayItemConvertationNode = convertationNode.GotoEachArrayElement(false);
                        if(arrayItemConvertationNode != null)
                        {
                            var setter = (EqualsToConfiguration)arrayItemConvertationNode.GetMutators().SingleOrDefault(mutator => mutator is EqualsToConfiguration);
                            if(setter != null)
                            {
                                var convertedArray = ConvertArray(array, setter.Value.Body);
                                return new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(convertedArray, null)};
                            }
                        }
                        return new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(array, null)};
                    }
                }
                return null;
            }
            var result = new List<KeyValuePair<Expression, Expression>>();
            bool wasUnconditionalSetter = false;
            for (int index = setters.Length - 1; index >= 0; --index)
            {
                var mutator = setters[index];
                LambdaExpression value;
                Expression condition;
                StaticValidatorConfiguration validator;
                var equalsToIfConfiguration = mutator as EqualsToIfConfiguration;
                if(equalsToIfConfiguration == null)
                {
                    if(wasUnconditionalSetter)
                        continue;
                    wasUnconditionalSetter = true;
                    var equalsToConfiguration = (EqualsToConfiguration)mutator;
                    value = equalsToConfiguration.Value;
                    condition = null;
                    validator = equalsToConfiguration.Validator;
                }
                else
                {
                    value = equalsToIfConfiguration.Value;
                    condition = lambda.Merge(Perform(equalsToIfConfiguration.Condition)).Body;
                    validator = equalsToIfConfiguration.Validator;
                }
                if(validator != null)
                {
                    if(aliases != null)
                    {
                        var validationResult = validator.Apply(aliases);
                        if(validationResult != null)
                        {
                            validationResult = Expression.Coalesce(validationResult, Expression.Constant(ValidationResult.Ok));
                            var valueIsValid = Expression.NotEqual(Expression.MakeMemberAccess(validationResult, validationResultTypeProperty), Expression.Constant(ValidationResultType.Error));
                            condition = condition == null ? valueIsValid : Expression.AndAlso(Convert(condition, typeof(bool)), valueIsValid);
                        }
                    }
                }
                result.Add(new KeyValuePair<Expression, Expression>(lambda.Merge(Perform(value)).Body, condition));
            }
            return result;
        }

        private static Expression ConvertArray(Expression array, Expression expression)
        {
            var itemType = array.Type.GetItemType();
            var parameter = Expression.Parameter(itemType);
            expression = expression.ResolveAliases(new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(itemType), array))});
            if(expression.NodeType == ExpressionType.Parameter)
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
            foreach(var shard in shards)
            {
                switch(shard.NodeType)
                {
                case ExpressionType.MemberAccess:
                    exp = Expression.MakeMemberAccess(exp, ((MemberExpression)shard).Member);
                    break;
                case ExpressionType.ArrayIndex:
                    exp = Expression.ArrayIndex(exp, ((BinaryExpression)shard).Right);
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)shard;
                    if(methodCallExpression.Method.IsCurrentMethod() || methodCallExpression.Method.IsEachMethod() || methodCallExpression.Method.IsTemplateIndexMethod())
                        exp = Expression.Call(methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(exp.Type.GetItemType()), exp);
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
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Expression.Convert(expression, type);
            return expression;
        }

        private Expression ResolveChain(Expression node)
        {
            var conditionalSetters = GetConditionalSetters(node);
            if(conditionalSetters == null)
                return Expression.Constant(node.Type.GetDefaultValue(), node.Type);
            var unconditionalSetter = conditionalSetters.SingleOrDefault(pair => pair.Value == null);
            Expression result = ConvertToNullable(unconditionalSetter.Key ?? Expression.Constant(node.Type.GetDefaultValue(), node.Type), node.Type);
            foreach(var item in conditionalSetters)
            {
                var condition = item.Value;
                var value = ConvertToNullable(item.Key, node.Type);
                if(condition == null)
                    continue;
                var test = Convert(condition, typeof(bool));
                result = Expression.Condition(test, value, result);
            }
            return result;
        }

        private static bool IsSimpleLinkOfChain(MethodCallExpression node, out Type type)
        {
            type = null;
            return node != null && (node.Method.IsCurrentMethod() || node.Method.IsEachMethod() || node.Method.IsTemplateIndexMethod() || node.Method.IsWhereMethod()) && IsSimpleLinkOfChain(node.Arguments.First(), out type);
        }

        private static bool IsSimpleLinkOfChain(MemberExpression node, out Type type)
        {
            type = null;
            return node != null && node.Member != stringLengthProperty && IsSimpleLinkOfChain(node.Expression, out type);
        }

        private static bool IsSimpleLinkOfChain(Expression node, out Type type)
        {
            type = null;
            if(node != null && node.NodeType == ExpressionType.Parameter)
                type = node.Type;
            return node != null && (node.NodeType == ExpressionType.Parameter
                                    || IsSimpleLinkOfChain(node as MemberExpression, out type)
                                    || (node.NodeType == ExpressionType.ArrayIndex && IsSimpleLinkOfChain(((BinaryExpression)node).Left, out type))
                                    || IsSimpleLinkOfChain(node as MethodCallExpression, out type));
        }

        private Type From { get; set; }
        private Type To { get; set; }

        // ReSharper disable StaticFieldInGenericType
        private static readonly MethodInfo selectMethod = ((MethodCallExpression)((Expression<Func<int[], IEnumerable<int>>>)(ints => ints.Select(i => i + 1))).Body).Method.GetGenericMethodDefinition();
        private static readonly MemberInfo stringLengthProperty = ((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member;
        private static readonly MethodInfo toArrayMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, int[]>>)(enumerable => enumerable.ToArray())).Body).Method.GetGenericMethodDefinition();
        private static readonly MemberInfo validationResultTypeProperty = ((MemberExpression)((Expression<Func<ValidationResult, ValidationResultType>>)(result => result.Type)).Body).Member;
        private static readonly MethodInfo whereMethod = ((MethodCallExpression)((Expression<Func<int[], IEnumerable<int>>>)(ints => ints.Where(x => x == 0))).Body).Method.GetGenericMethodDefinition();
        // ReSharper restore StaticFieldInGenericType

        private readonly LambdaExpression lambda;

        private bool resolved;
        private readonly ModelConfigurationNode convertationTree;
        private readonly List<KeyValuePair<Expression, Expression>> aliases;
    }
}