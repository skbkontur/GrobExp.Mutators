using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public static class ExpressionExtensions
    {
        public static bool IsAnonymousTypeCreation(this Expression node)
        {
            return node != null && node.NodeType == ExpressionType.New && node.Type.IsAnonymousType();
        }

        public static bool IsLinkOfChain(this Expression node, bool rootOnlyParameter, bool hard)
        {
            return node != null &&
                   (node.NodeType == ExpressionType.Parameter
                    || (node.NodeType == ExpressionType.Constant && !rootOnlyParameter)
                    || IsLinkOfChain(node as MemberExpression, rootOnlyParameter, hard)
                    || node.NodeType == ExpressionType.ArrayIndex
                    || IsLinkOfChain(node as MethodCallExpression, rootOnlyParameter, hard));
        }

        public static bool IsNull(this Expression node)
        {
            return node.NodeType == ExpressionType.Constant && ((ConstantExpression)node).Value == null;
        }

        public static Expression ExtendSelectMany(this Expression expression)
        {
            return new SelectManyCollectionSelectorExtender().Visit(expression);
        }

        public static Expression ExtendNulls(this Expression expression)
        {
            return new ExpressionNullCheckingExtender().Extend(expression);
        }

        public static ParameterExpression[] ExtractParameters(this Expression expression)
        {
            return new ParametersExtractor().Extract(expression);
        }

        public static bool HasParameter(this Expression expression, HashSet<ParameterExpression> parameters)
        {
            return new ParameterExistanceChecker(parameters).HasParameter(expression);
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
            //return LambdaExpressionCreator.Create<Func<TRoot, TValue>>(new ExpressionMerger(path1, path2).Merge(path).Body, path1.Parameters[0]);
        }

        public static Expression<Func<TRoot, TValue>> Merge<TRoot, T1, T2, T3, TValue>(this Expression<Func<T1, T2, T3, TValue>> path, Expression<Func<TRoot, T1>> path1, Expression<Func<TRoot, T2>> path2, Expression<Func<TRoot, T3>> path3)
        {
            return new ExpressionMerger(path1, path2, path3).Merge<Func<TRoot, TValue>>(path);
            //return LambdaExpressionCreator.Create<Func<TRoot, TValue>>(new ExpressionMerger(path1, path2, path3).Merge(path).Body, path1.Parameters[0]);
        }

        public static Expression<Func<TRoot, TValue>> ReplaceParameter<TRoot, TValue>(this Expression<Func<TRoot, TValue>> expression, ParameterExpression newParameter)
        {
            if(newParameter.Type != typeof(TRoot))
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
            if(exp.Type != typeof(object))
                exp = Expression.Convert(exp, typeof(object));
            //exp = exp.ExtendNulls();
            return ExpressionCompiler.Compile(Expression.Lambda<Func<object>>(exp))();
        }

        public static Expression Simplify(this Expression expression)
        {
            return new ExpressionSimplifier().Simplify(expression);
        }

        public static bool IsConstant(this Expression exp)
        {
            return new IsConstantChecker().IsConstant(exp);
        }

        public static bool IsStringLengthPropertyAccess(this Expression expression)
        {
            return expression.NodeType == ExpressionType.MemberAccess && ((MemberExpression)expression).Member == stringLengthProperty;
        }

        public static LambdaExpression AndAlso(this LambdaExpression left, LambdaExpression right, bool convertToNullable = true)
        {
            if(left == null) return right;
            if(right == null) return left;
            Expression leftBody, rightBody;
            ParameterExpression[] parameters;
            Canonize(left, right, out leftBody, out rightBody, out parameters);
            return Expression.Lambda(Expression.AndAlso(convertToNullable ? Convert(leftBody) : leftBody, convertToNullable ? Convert(rightBody) : rightBody), parameters);
        }

        public static LambdaExpression OrElse(this LambdaExpression left, LambdaExpression right)
        {
            if(left == null) return right;
            if(right == null) return left;
            Expression leftBody, rightBody;
            ParameterExpression[] parameters;
            Canonize(left, right, out leftBody, out rightBody, out parameters);
            return Expression.Lambda(Expression.OrElse(Convert(leftBody), Convert(rightBody)), parameters);
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
            if(lambda == null) return new LambdaExpression[0];
            new DependenciesExtractor(lambda, lambda.Parameters).Extract(out primaryDependencies, out additionalDependencies);
            return primaryDependencies;
        }

        public static LambdaExpression[] ExtractDependencies(this LambdaExpression lambda, IEnumerable<ParameterExpression> parameters)
        {
            var parametersArray = parameters.ToArray();
            if(parametersArray.Length == 0)
                return new LambdaExpression[0];
            LambdaExpression[] primaryDependencies;
            LambdaExpression[] additionalDependencies;
            return lambda == null ? new LambdaExpression[0] : new DependenciesExtractor(lambda, parametersArray).Extract(out primaryDependencies, out additionalDependencies);
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

        public static Expression ResolveInterfaceMembers(this Expression expression)
        {
            return new InterfaceMemberResolver().Visit(expression);
        }

        public static LambdaExpression ResolveAbstractPath(LambdaExpression path, LambdaExpression abstractPath)
        {
            var parameters = new List<PathPrefix> {new PathPrefix(path.Body, path.Parameters[0])};
            return Expression.Lambda(new AbstractPathResolver(parameters, false).Resolve(abstractPath.Body), path.Parameters);
        }

        public static Expression ResolveAbstractPath(this Expression expression, List<PathPrefix> pathPrefixes)
        {
            if(pathPrefixes == null || pathPrefixes.Count == 0) return expression;
            return new AbstractPathResolver(pathPrefixes, true).Resolve(expression);
        }

        public static Expression ResolveAliases(this Expression expression, List<KeyValuePair<Expression, Expression>> aliases, bool strictly = false)
        {
            if(aliases == null || aliases.Count == 0)
                return expression;
            return new AliasesResolver(aliases, strictly).Visit(expression);
        }

        public static Expression ResolveArrayIndexes(this Expression exp)
        {
            return new ArrayIndexResolver().Resolve(exp);
        }

        public static Expression[] SmashToSmithereens(this Expression exp)
        {
            var result = new List<Expression>();
            while(exp != null)
            {
                var end = false;
                result.Add(exp);
                switch(exp.NodeType)
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
                default:
                    end = true;
                    break;
                }
                if(end) break;
            }
            result.Reverse();
            return result.ToArray();
        }

        private static Expression LCP(this Expression exp1, Expression exp2)
        {
            if(exp1 == null || exp2 == null) return null;
            var shards1 = exp1.SmashToSmithereens();
            var shards2 = exp2.SmashToSmithereens();
            int i;
            for(i = 0; i < shards1.Length && i < shards2.Length; ++i)
            {
                if(!ExpressionEquivalenceChecker.Equivalent(shards1[i], shards2[i], false))
                    break;
            }
            return i == 0 ? null : shards1[i - 1];
        }

        private static Expression Convert(Expression exp)
        {
            return exp.Type == typeof(bool?) ? exp : Expression.Convert(exp, typeof(bool?));
        }

        private static void Canonize(LambdaExpression left, LambdaExpression right, out Expression leftBody, out Expression rightBody, out ParameterExpression[] parameters)
        {
            var leftParameters = new Dictionary<Type, ParameterExpression>();
            foreach(var parameter in left.Parameters)
            {
                if(leftParameters.ContainsKey(parameter.Type))
                    throw new InvalidOperationException("Two parameters of the same type are prohibited");
                leftParameters.Add(parameter.Type, parameter);
            }
            var rightParameters = new Dictionary<Type, ParameterExpression>();
            foreach(var parameter in right.Parameters)
            {
                if(rightParameters.ContainsKey(parameter.Type))
                    throw new InvalidOperationException("Two parameters of the same type are prohibited");
                rightParameters.Add(parameter.Type, parameter);
            }
            foreach(var item in rightParameters)
            {
                if(!leftParameters.ContainsKey(item.Key))
                    leftParameters.Add(item.Key, item.Value);
            }
            leftBody = left.Body;
            rightBody = right.Body;
            foreach(var parameter in right.Parameters)
            {
                if(leftParameters[parameter.Type] != parameter)
                    rightBody = new ParameterReplacer(parameter, leftParameters[parameter.Type]).Visit(rightBody);
            }
            parameters = leftParameters.Values.ToArray();
        }

        private static bool IsLinkOfChain(MemberExpression node, bool rootOnlyParameter, bool hard)
        {
            if(hard)
                return node != null && IsLinkOfChain(node.Expression, rootOnlyParameter, true);
            return node != null && node.Expression != null;
        }

        private static bool IsLinkOfChain(MethodCallExpression node, bool rootOnlyParameter, bool hard)
        {
            if(hard)
                return node != null && ((node.Object != null && IsLinkOfChain(node.Object, rootOnlyParameter, true)) || (node.Method.IsExtension() && IsLinkOfChain(node.Arguments[0], rootOnlyParameter, true)));
            return node != null && (node.Object != null || node.Method.IsExtension());
        }

        private static readonly MemberInfo stringLengthProperty = ((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member;

        private static readonly MethodInfo firstWithoutParametersMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.First())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo firstWithParametersMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.First(i => i == 0))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo firstOrDefaultWithoutParametersMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.FirstOrDefault())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo firstOrDefaultWithParametersMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.FirstOrDefault(i => i == 0))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo singleWithoutParametersMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.Single())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo singleWithParametersMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.Single(i => i == 0))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo singleOrDefaultWithoutParametersMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.SingleOrDefault())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo singleOrDefaultWithParametersMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => arr.SingleOrDefault(i => i == 0))).Body).Method.GetGenericMethodDefinition();
    }
}