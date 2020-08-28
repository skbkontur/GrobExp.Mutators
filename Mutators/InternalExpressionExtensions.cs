using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using GrobExp.Compiler;
using GrobExp.Mutators.MutatorsRecording;
using GrobExp.Mutators.MutatorsRecording.AssignRecording;
using GrobExp.Mutators.Visitors;

using GroBuf.Readers;

using JetBrains.Annotations;

namespace GrobExp.Mutators
{
    internal static class InternalExpressionExtensions
    {
        public static bool IsOfType(this Expression node, ExpressionType type)
        {
            return node != null && node.NodeType == type;
        }

        public static bool IsTupleCreation(this Expression node)
        {
            return node != null && node.NodeType == ExpressionType.New && node.Type.IsTuple();
        }

        public static Expression AddToDictionary(this Expression exp, Expression key, Expression value)
        {
            return Expression.IfThenElse(
                Expression.Call(exp, "ContainsKey", null, key),
                Expression.Call(exp, exp.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetSetMethod(), key, value),
                Expression.Call(exp, "Add", null, key, value));
        }

        public static List<Expression> SplitToBatches(this List<Expression> expressions, params ParameterExpression[] parameters)
        {
            var result = new List<Expression>();
            var expressionsCounter = new ExpressionNodesCounter();

            var currentBatch = new List<Expression>();
            var expressionsInBatch = 0;

            foreach (var expression in expressions)
            {
                var count = expressionsCounter.Count(expression);
                if (expressionsInBatch + count > maxNumberOfExpressionsInBatch)
                {
                    if (currentBatch.Count > 0)
                        result.Add(MakeInvocation(currentBatch, parameters));
                    currentBatch.Clear();
                    expressionsInBatch = 0;
                }

                expressionsInBatch += count;
                currentBatch.Add(expression);
            }

            if (currentBatch.Count > 0)
                result.Add(MakeInvocation(currentBatch, parameters));
            return result;
        }

        public static Expression<TDelegate> EliminateLinq<TDelegate>(this Expression<TDelegate> expression)
        {
            return Expression.Lambda<TDelegate>(expression.Body.EliminateLinq(), expression.Parameters);
        }

        public static Expression EliminateLinq(this Expression expression)
        {
            return expression == null ? null : new LinqEliminator().Eliminate(expression);
        }

        internal static Expression Assign(this Expression path, Type converterType, Expression value, AssignLogInfo toLog = null)
        {
            if (!MutatorsAssignRecorder.IsRecording() || toLog == null)
                return path.InternalAssign(converterType, value);
            MutatorsAssignRecorder.RecordCompilingExpression(converterType, toLog);
            MethodCallExpression recordingExpression;
            var temp = Expression.Variable(value.Type, "temp");

            if (value.Type.IsNullable())
            {
                recordingExpression = Expression.Call(RecordingMethods.RecordExecutingExpressionWithNullableValueCheckGenericMethodInfo.MakeGenericMethod(Nullable.GetUnderlyingType(value.Type)), Expression.Constant(converterType, typeof(Type)), Expression.Constant(toLog), value);
            }
            else if (value.Type.IsValueType)
                recordingExpression = Expression.Call(RecordingMethods.RecordExecutingExpressionMethodInfo, Expression.Constant(converterType, typeof(Type)), Expression.Constant(toLog));
            else
                recordingExpression = Expression.Call(RecordingMethods.RecordExecutingExpressionWithValueObjectCheck, Expression.Constant(converterType, typeof(Type)), Expression.Constant(toLog), value);

            return Expression.Block(new[] {temp},
                                    Expression.Assign(temp, value),
                                    path.InternalAssign(converterType, temp),
                                    recordingExpression,
                                    temp);
        }

        public static string CustomFieldName(this Expression node)
        {
            var result = new StringBuilder();
            BuildCustomFieldName(node, result);
            return string.Intern(result.ToString());
        }

        public static Expression ExtendSelectMany(this Expression expression)
        {
            return new SelectManyCollectionSelectorExtender().Visit(expression);
        }

        public static bool HasParameter(this Expression expression, HashSet<ParameterExpression> parameters)
        {
            return new ParameterExistanceChecker(parameters).HasParameter(expression);
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

        /// <inheritdoc cref="IsConstantChecker.IsConstant" />
        public static bool IsConstant([NotNull] this Expression exp)
        {
            return new IsConstantChecker().IsConstant(exp);
        }

        public static bool IsStringLengthPropertyAccess(this Expression expression)
        {
            return expression.NodeType == ExpressionType.MemberAccess && ((MemberExpression)expression).Member == stringLengthProperty;
        }

        public static LambdaExpression[] ExtractDependencies(this LambdaExpression lambda)
        {
            return lambda == null ? new LambdaExpression[0] : new DependenciesExtractor(lambda, lambda.Parameters).Extract(out _, out _);
        }

        public static LambdaExpression[] ExtractDependencies(this LambdaExpression lambda, IEnumerable<ParameterExpression> parameters)
        {
            var parametersArray = parameters.ToArray();
            if (parametersArray.Length == 0)
                return new LambdaExpression[0];
            return lambda == null ? new LambdaExpression[0] : new DependenciesExtractor(lambda, parametersArray).Extract(out _, out _);
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
            return new MethodReplacer(
                (From : firstWithoutParametersMethod, To : firstOrDefaultWithoutParametersMethod),
                (From : firstWithParametersMethod, To : firstOrDefaultWithParametersMethod),
                (From : singleWithoutParametersMethod, To : singleOrDefaultWithoutParametersMethod),
                (From : singleWithParametersMethod, To : singleOrDefaultWithParametersMethod)
            ).Visit(expression);
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
            return new MethodReplacer(from : from, to : to).Visit(expression);
        }

        public static Expression ReplaceCurrentWithEach(this Expression expression)
        {
            return currentToEachReplacer.Visit(expression);
        }

        public static LambdaExpression ResolveAbstractPath(this LambdaExpression path, LambdaExpression abstractPath)
        {
            var parameters = new List<PathPrefix> {new PathPrefix(path.Body, path.Parameters[0])};
            return Expression.Lambda(new AbstractPathResolver(parameters, false).Resolve(abstractPath.Body), path.Parameters);
        }

        public static Expression ResolveAbstractPath(this Expression expression, List<PathPrefix> pathPrefixes)
        {
            if (pathPrefixes == null || pathPrefixes.Count == 0) return expression;
            return new AbstractPathResolver(pathPrefixes, true).Resolve(expression);
        }

        public static Expression ResolveAliasesInLambda(this LambdaExpression expression, List<KeyValuePair<Expression, Expression>> aliases)
        {
            if (aliases == null || aliases.Count == 0)
                return expression;
            return new LambdaAliasesResolver(aliases).Resolve(expression);
        }

        public static Expression LCP(this Expression exp1, Expression exp2)
        {
            if (exp1 == null || exp2 == null) return null;
            var shards1 = exp1.SmashToSmithereens();
            var shards2 = exp2.SmashToSmithereens();
            int i;
            for (i = 0; i < shards1.Length && i < shards2.Length; ++i)
            {
                if (!ExpressionEquivalenceChecker.Equivalent(shards1[i], shards2[i], false, true))
                    break;
            }

            return i == 0 ? null : shards1[i - 1];
        }

        private static void BuildCustomFieldName(Expression node, StringBuilder result)
        {
            switch (node.NodeType)
            {
            case ExpressionType.Parameter:
                return;
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)node;
                BuildCustomFieldName(memberExpression.Expression, result);
                if (result.Length > 0)
                    result.Append("ё");
                result.Append(string.Intern(memberExpression.Member.Name));
                break;
            case ExpressionType.Call:
                var methodCallExpression = (MethodCallExpression)node;
                if (methodCallExpression.Method.IsEachMethod() || methodCallExpression.Method.IsCurrentMethod())
                {
                    BuildCustomFieldName(methodCallExpression.Arguments.Single(), result);
                    break;
                }

                throw new InvalidOperationException($"The method '{methodCallExpression.Method}' is not supported");
            default:
                throw new InvalidOperationException($"Node type '{node.NodeType}' is not supported");
            }
        }

        private static Expression InternalAssign(this Expression path, Type converterType, Expression value)
        {
            if (path.NodeType == ExpressionType.Convert)
                path = ((UnaryExpression)path).Operand;
            switch (path.NodeType)
            {
            case ExpressionType.ArrayIndex:
                var binaryExpression = (BinaryExpression)path;
                path = Expression.ArrayAccess(binaryExpression.Left, binaryExpression.Right);
                break;
            case ExpressionType.MemberAccess:
                {
                    var memberExpression = (MemberExpression)path;
                    if (memberExpression.Expression.Type.IsArray && memberExpression.Member.Name == "Length")
                    {
                        var temp = Expression.Variable(memberExpression.Expression.Type);
                        return Expression.Block(new[] {temp},
                                                Expression.Assign(temp, memberExpression.Expression),
                                                Expression.Call(arrayResizeMethod.MakeGenericMethod(memberExpression.Expression.Type.GetElementType()), temp, value),
                                                Expression.Assign(memberExpression.Expression, temp));
                    }

                    if (memberExpression.Expression.Type.IsGenericType && memberExpression.Expression.Type.GetGenericTypeDefinition() == typeof(List<>) && memberExpression.Member.Name == "Count")
                        return Expression.Call(listResizeMethod.MakeGenericMethod(memberExpression.Expression.Type.GetItemType()), memberExpression.Expression, value);
                }
                break;
            case ExpressionType.Call:
                var methodCallExpression = (MethodCallExpression)path;
                if (methodCallExpression.Method.IsIndexerGetter())
                {
                    var temp = Expression.Variable(methodCallExpression.Object.Type);
                    Expression assignment;
                    if (!methodCallExpression.Object.Type.IsDictionary())
                        assignment = Expression.Call(temp, "set_Item", null, methodCallExpression.Arguments.Single(), value);
                    else
                    {
                        assignment = Expression.Call(temp, "set_Item", null, methodCallExpression.Arguments.Single(), value);
//                        assignment = Expression.IfThenElse(
//                            Expression.Call(temp, "ContainsKey", null, methodCallExpression.Arguments.Single()),
//                            ,
//                            Expression.Call(temp, "Add", null, methodCallExpression.Arguments.Single(), value))
                    }

                    return Expression.Block(new[] {temp},
                                            Expression.Assign(temp, methodCallExpression.Object),
                                            Expression.IfThen(Expression.Equal(temp, Expression.Constant(null, temp.Type)), Expression.Assign(temp, Expression.Convert(methodCallExpression.Object.Assign(converterType, Expression.New(temp.Type)), temp.Type))),
                                            assignment);
                }

                break;
            }

            return Expression.Assign(path, value);
        }

        private static InvocationExpression MakeInvocation(List<Expression> currentBatch, params ParameterExpression[] parameters)
        {
            return Expression.Invoke(Expression.Lambda(Expression.Block(currentBatch), parameters), parameters.Cast<Expression>());
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
                var f = ObjectConstructionHelper.ConstructType(typeof(T));
                while (list.Count < size)
                    list.Add((T)f());
            }
        }

        private const int maxNumberOfExpressionsInBatch = 1000;

        private static readonly MethodReplacer currentToEachReplacer = new MethodReplacer(from : MutatorsHelperFunctions.CurrentMethod, to : MutatorsHelperFunctions.EachMethod);

        private static readonly MethodInfo arrayResizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo listResizeMethod = ((MethodCallExpression)((Expression<Action<List<int>>>)(arr => Resize(arr, 0))).Body).Method.GetGenericMethodDefinition();

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