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

        private static readonly MethodReplacer eachToCurrentReplacer = new MethodReplacer(from: MutatorsHelperFunctions.EachMethod, to: MutatorsHelperFunctions.CurrentMethod);
    }
}