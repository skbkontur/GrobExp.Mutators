using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators.Visitors;

using JetBrains.Annotations;

namespace GrobExp.Mutators
{
    public static class ExpressionExtensions
    {
        public static ParameterExpression[] ExtractParameters(this Expression expression)
        {
            return new ParametersExtractor().Extract(expression);
        }

        [NotNull]
        public static Expression CanonizeParameters([NotNull] this Expression expression)
        {
            return new ParameterCanonizer().Canonize(expression);
        }

        public static Expression ReplaceEachWithCurrent(this Expression expression)
        {
            return eachToCurrentReplacer.Visit(expression);
        }

        public static bool IsNull(this Expression node)
        {
            return node is ConstantExpression constantExpression && constantExpression.Value == null;
        }

        public static Expression Simplify(this Expression expression)
        {
            return new ExpressionSimplifier().Simplify(expression);
        }

        public static LambdaExpression Merge(this LambdaExpression pathToChild, LambdaExpression pathToValue)
        {
            return new ExpressionMerger(pathToChild).Merge(pathToValue);
        }

        public static Expression<Func<TRoot, TValue>> Merge<TRoot, TChild, TValue>(this Expression<Func<TRoot, TChild>> pathToChild, Expression<Func<TChild, TValue>> pathToValue)
        {
            return new ExpressionMerger(pathToChild).Merge<Func<TRoot, TValue>>(pathToValue);
        }

        public static Expression<Func<TRoot, TValue>> Merge<TRoot, T1, T2, TValue>(this Expression<Func<T1, T2, TValue>> path, Expression<Func<TRoot, T1>> path1, Expression<Func<TRoot, T2>> path2)
        {
            return new ExpressionMerger(path1, path2).Merge<Func<TRoot, TValue>>(path);
        }

        public static Expression<Func<TRoot, TValue>> Merge<TRoot, T1, T2, T3, TValue>(this Expression<Func<T1, T2, T3, TValue>> path, Expression<Func<TRoot, T1>> path1, Expression<Func<TRoot, T2>> path2, Expression<Func<TRoot, T3>> path3)
        {
            return new ExpressionMerger(path1, path2, path3).Merge<Func<TRoot, TValue>>(path);
        }

        public static Expression<Func<TRoot1, TRoot2, TValue>> MergeFrom2Roots<TRoot1, TRoot2, T1, T2, TValue>(this Expression<Func<T1, T2, TValue>> path, Expression<Func<TRoot1, T1>> path1, Expression<Func<TRoot2, T2>> path2)
        {
            return new ExpressionMerger(path1, path2).Merge<Func<TRoot1, TRoot2, TValue>>(path);
        }

        public static Expression<Func<TRoot1, TRoot2, TContext, TValue>> MergeFrom2RootsAndContext<TRoot1, TRoot2, TContext, T1, T2, TValue>(this Expression<Func<T1, T2, TContext, TValue>> path, Expression<Func<TRoot1, T1>> path1, Expression<Func<TRoot2, T2>> path2)
        {
            Expression<Func<TContext, TContext>> exp = x => x;
            return new ExpressionMerger(path1, path2, exp).Merge<Func<TRoot1, TRoot2, TContext, TValue>>(path);
        }

        public static Expression<Func<TRoot, TValue>> ReplaceParameter<TRoot, TValue>(this Expression<Func<TRoot, TValue>> expression, ParameterExpression newParameter)
        {
            if (newParameter.Type != typeof(TRoot))
                throw new ArgumentException("Parameter of type '" + typeof(TRoot) + "' expected");
            return Expression.Lambda<Func<TRoot, TRoot>>(newParameter, newParameter).Merge(expression);
        }

        public static ConstantExpression ToConstant(this Expression exp)
        {
            var constantExpression = Expression.Constant(Evaluate(exp), exp.Type);
            return constantExpression;
        }

        public static object Evaluate(this Expression exp)
        {
            if (exp.Type != typeof(object))
                exp = Expression.Convert(exp, typeof(object));
            return ExpressionCompiler.Compile(Expression.Lambda<Func<object>>(exp))();
        }

        public static Expression Simplify(this Expression expression)
        {
            return new ExpressionSimplifier().Simplify(expression);
        }

        /// <inheritdoc cref="IsConstantChecker.IsConstant" />
        public static bool IsConstant([NotNull] this Expression exp)
        {
            return new IsConstantChecker().IsConstant(exp);
        }

        public static bool IsStringLengthPropertyAccess(this Expression expression)
        {
            return expression.NodeType == ExpressionType.MemberAccess && ((MemberExpression)expression).Member == stringLengthProperty;
        }

        public static LambdaExpression AndAlso(this LambdaExpression left, LambdaExpression right, bool convertToNullable = true)
        {
            if (left == null) return right;
            if (right == null) return left;
            Expression leftBody, rightBody;
            ParameterExpression[] parameters;
            Canonize(left, right, out leftBody, out rightBody, out parameters);
            return Expression.Lambda(Expression.AndAlso(convertToNullable ? Convert(leftBody) : leftBody, convertToNullable ? Convert(rightBody) : rightBody), parameters);
        }

        public static LambdaExpression OrElse(this LambdaExpression left, LambdaExpression right, bool convertToNullable = true)
        {
            if (left == null) return right;
            if (right == null) return left;
            Expression leftBody, rightBody;
            ParameterExpression[] parameters;
            Canonize(left, right, out leftBody, out rightBody, out parameters);
            return Expression.Lambda(Expression.OrElse(convertToNullable ? Convert(leftBody) : leftBody, convertToNullable ? Convert(rightBody) : rightBody), parameters);
        }

        public static LambdaExpression[] ExtractDependencies(this LambdaExpression lambda)
        {
            LambdaExpression[] primaryDependencies;
            LambdaExpression[] additionalDependencies;
            return lambda == null ? new LambdaExpression[0] : new DependenciesExtractor(lambda, lambda.Parameters).Extract(out primaryDependencies, out additionalDependencies);
        }

        public static LambdaExpression[] ExtractPrimaryDependencies(this LambdaExpression lambda)
        {
            LambdaExpression[] primaryDependencies;
            LambdaExpression[] additionalDependencies;
            if (lambda == null) return new LambdaExpression[0];
            new DependenciesExtractor(lambda, lambda.Parameters).Extract(out primaryDependencies, out additionalDependencies);
            return primaryDependencies;
        }

        public static LambdaExpression[] ExtractDependencies(this LambdaExpression lambda, IEnumerable<ParameterExpression> parameters)
        {
            var parametersArray = parameters.ToArray();
            if (parametersArray.Length == 0)
                return new LambdaExpression[0];
            LambdaExpression[] primaryDependencies;
            LambdaExpression[] additionalDependencies;
            return lambda == null ? new LambdaExpression[0] : new DependenciesExtractor(lambda, parametersArray).Extract(out primaryDependencies, out additionalDependencies);
        }

        public static Expression CleanFilters(this Expression node, List<LambdaExpression> filters)
        {
            var shards = node.SmashToSmithereens();
            var result = shards[0];
            var i = 0;
            while (i + 1 < shards.Length)
            {
                ++i;
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
                    if (methodCallExpression.Method.IsWhereMethod() && i + 1 < shards.Length && shards[i + 1].NodeType == ExpressionType.Call && (((MethodCallExpression)shards[i + 1]).Method.IsCurrentMethod() || ((MethodCallExpression)shards[i + 1]).Method.IsEachMethod()))
                    {
                        result = Expression.Call(((MethodCallExpression)shards[i + 1]).Method, result);
                        filters.Add(Expression.Lambda(result, (ParameterExpression)shards[0]).Merge((LambdaExpression)methodCallExpression.Arguments[1]));
                        ++i;
                    }
                    else
                    {
                        if (methodCallExpression.Method.IsCurrentMethod() || methodCallExpression.Method.IsEachMethod())
                            filters.Add(null);
                        result = methodCallExpression.Method.IsStatic
                                     ? Expression.Call(methodCallExpression.Method, new[] {result}.Concat(methodCallExpression.Arguments.Skip(1)))
                                     : Expression.Call(result, methodCallExpression.Method, methodCallExpression.Arguments);
                    }

                    break;
                case ExpressionType.Convert:
                    result = Expression.Convert(result, shard.Type);
                    break;
                case ExpressionType.Coalesce:
                    result = Expression.Coalesce(result, ((BinaryExpression)shard).Right);
                    break;
                default:
                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
                }
            }

            return result;
        }

        public static Expression[] CutToChains(this Expression expression, bool rootOnlyParameter, bool hard)
        {
            return new ExpressionRipper().Cut(expression, rootOnlyParameter, hard);
        }

        public static Expression FindLCP(this IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate((Expression)null, (current, exp) => current == null ? exp : current.LCP(exp));
        }

        public static Expression RemoveLinqFirstAndSingle(this Expression expression)
        {
            expression = new MethodReplacer(firstWithoutParametersMethod, firstOrDefaultWithoutParametersMethod).Visit(expression);
            expression = new MethodReplacer(firstWithParametersMethod, firstOrDefaultWithParametersMethod).Visit(expression);
            expression = new MethodReplacer(singleWithoutParametersMethod, singleOrDefaultWithoutParametersMethod).Visit(expression);
            expression = new MethodReplacer(singleWithParametersMethod, singleOrDefaultWithParametersMethod).Visit(expression);
            return expression;
        }

        public static LambdaExpression ResolveInterfaceMembers(this LambdaExpression lambda)
        {
            return Expression.Lambda(lambda.Body.ResolveInterfaceMembers(), lambda.Parameters);
        }

        public static Expression ResolveInterfaceMembers(this Expression expression)
        {
            return new InterfaceMemberResolver().Visit(expression);
        }

        public static Expression ReplaceMethod(this Expression expression, MethodInfo from, MethodInfo to)
        {
            return new MethodReplacer(from, to).Visit(expression);
        }

        public static LambdaExpression ResolveAbstractPath(LambdaExpression path, LambdaExpression abstractPath)
        {
            var parameters = new List<PathPrefix> {new PathPrefix(path.Body, path.Parameters[0])};
            return Expression.Lambda(new AbstractPathResolver(parameters, false).Resolve(abstractPath.Body), path.Parameters);
        }

        public static Expression ResolveAbstractPath(this Expression expression, List<PathPrefix> pathPrefixes)
        {
            if (pathPrefixes == null || pathPrefixes.Count == 0) return expression;
            return new AbstractPathResolver(pathPrefixes, true).Resolve(expression);
        }

        public static Expression ResolveAliases(this Expression expression, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (aliases == null || aliases.Count == 0)
                return expression;
            return new AliasesResolver(aliases).Visit(expression);
        }

        public static Expression ResolveAliasesInLambda(this LambdaExpression expression, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (aliases == null || aliases.Count == 0)
                return expression;
            return new LambdaAliasesResolver(aliases).Resolve(expression);
        }

        public static Expression[] SmashToSmithereens(this Expression exp)
        {
            var result = new List<Expression>();
            while (exp != null)
            {
                var end = false;
                result.Add(exp);
                switch (exp.NodeType)
                {
                case ExpressionType.MemberAccess:
                    exp = ((MemberExpression)exp).Expression;
                    break;
                case ExpressionType.ArrayIndex:
                    exp = ((BinaryExpression)exp).Left;
                    break;
                case ExpressionType.ArrayLength:
                    exp = ((UnaryExpression)exp).Operand;
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)exp;
                    exp = methodCallExpression.Method.IsExtension() ? methodCallExpression.Arguments[0] : methodCallExpression.Object;
                    break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    exp = ((UnaryExpression)exp).Operand;
                    break;
                case ExpressionType.Coalesce:
                    exp = ((BinaryExpression)exp).Left;
                    break;
                default:
                    end = true;
                    break;
                }

                if (end) break;
            }

            result.Reverse();
            return result.ToArray();
        }

        public static string CustomFieldName(this LambdaExpression lambda)
        {
            return lambda.Body.CustomFieldName();
        }

        public static LambdaExpression[] ExtractPrimaryDependencies(this LambdaExpression lambda)
        {
            if (lambda == null)
                return new LambdaExpression[0];
            new DependenciesExtractor(lambda, lambda.Parameters).Extract(out var primaryDependencies, out var additionalDependencies);
            return primaryDependencies;
        }

        public static bool IsAnonymousTypeCreation(this Expression node)
        {
            return node != null && node.NodeType == ExpressionType.New && node.Type.IsAnonymousType();
        }

        public static Expression ResolveAliases(this Expression expression, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (aliases == null || aliases.Count == 0)
                return expression;
            return new AliasesResolver(aliases).Visit(expression);
        }

        public static LambdaExpression AndAlso(this LambdaExpression left, LambdaExpression right, bool convertToNullable = true)
        {
            if (left == null)
                return right;
            if (right == null)
                return left;
            Canonize(left, right, out var leftBody, out var rightBody, out var parameters);
            return Expression.Lambda(Expression.AndAlso(convertToNullable ? Convert(leftBody) : leftBody, convertToNullable ? Convert(rightBody) : rightBody), parameters);
        }

        public static LambdaExpression OrElse(this LambdaExpression left, LambdaExpression right, bool convertToNullable = true)
        {
            if (left == null)
                return right;
            if (right == null)
                return left;
            Canonize(left, right, out var leftBody, out var rightBody, out var parameters);
            return Expression.Lambda(Expression.OrElse(convertToNullable ? Convert(leftBody) : leftBody, convertToNullable ? Convert(rightBody) : rightBody), parameters);
        }

        private static Expression Convert(Expression exp)
        {
            return exp.Type == typeof(bool?) ? exp : Expression.Convert(exp, typeof(bool?));
        }

        private static void Canonize(LambdaExpression left, LambdaExpression right, out Expression leftBody, out Expression rightBody, out ParameterExpression[] parameters)
        {
            var leftParameters = new Dictionary<Type, ParameterExpression>();
            foreach (var parameter in left.Parameters)
            {
                if (leftParameters.ContainsKey(parameter.Type))
                    throw new InvalidOperationException("Two parameters of the same type are prohibited");
                leftParameters.Add(parameter.Type, parameter);
            }

            var rightParameters = new Dictionary<Type, ParameterExpression>();
            foreach (var parameter in right.Parameters)
            {
                if (rightParameters.ContainsKey(parameter.Type))
                    throw new InvalidOperationException("Two parameters of the same type are prohibited");
                rightParameters.Add(parameter.Type, parameter);
            }

            foreach (var item in rightParameters)
            {
                if (!leftParameters.ContainsKey(item.Key))
                    leftParameters.Add(item.Key, item.Value);
            }

            leftBody = left.Body;
            rightBody = right.Body;
            foreach (var parameter in right.Parameters)
            {
                if (leftParameters[parameter.Type] != parameter)
                    rightBody = new ParameterReplacer(parameter, leftParameters[parameter.Type]).Visit(rightBody);
            }

            parameters = leftParameters.Values.ToArray();
        }

        private static readonly MethodReplacer eachToCurrentReplacer = new MethodReplacer(from : MutatorsHelperFunctions.EachMethod, to : MutatorsHelperFunctions.CurrentMethod);
    }
}