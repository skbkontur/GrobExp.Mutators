using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Compiler;

namespace GrobExp.Mutators.Visitors
{
    public class LinqEliminator : ExpressionVisitor
    {
        public Expression Eliminate(Expression node)
        {
            needGlobalIndexes = false;
            numberOfVariables = 0;
            return Visit(node);
        }

        public Expression Eliminate(Expression node, out ParameterExpression[] indexes)
        {
            needGlobalIndexes = true;
            this.indexes = new List<ParameterExpression>();
            var result = Visit(node);
            indexes = this.indexes.ToArray();
            return result;
        }

        public override Expression Visit(Expression node)
        {
            return node.IsLinkOfChain(false, false) ? VisitChain(DefaultFinishAction, null, node) : base.Visit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if(node.Method.DeclaringType == typeof(Enumerable))
            {
                switch(node.Method.Name)
                {
                case "ToArray":
                    return Expression.Call(Visit(node.Arguments.Single()), "ToArray", null, null);
                case "ToList":
                    var enumerable = Visit(node.Arguments.Single());
                    if(enumerable.Type.IsGenericType && enumerable.Type.GetGenericTypeDefinition() == typeof(List<>))
                        return enumerable;
                    var itemType = enumerable.Type.GetItemType();
                    return Expression.New(typeof(List<>).MakeGenericType(itemType).GetConstructor(new[] {typeof(IEnumerable<>).MakeGenericType(itemType)}), enumerable);
                }
                return Expression.Call(Visit(node.Object), node.Method.Name, null, node.Arguments.Select(Visit).ToArray());
            }
            return base.VisitMethodCall(node);
        }

        private Expression VisitChain(Action<Context, ParameterExpression, List<Expression>, List<ParameterExpression>> finishAction, Context parentContext, Expression node)
        {
            var smithereens = node.SmashToSmithereens();
            int i;
            for(i = 0; i < smithereens.Length; ++i)
            {
                if(IsLinqCall(smithereens[i]))
                    break;
            }
            node = base.Visit(smithereens[i - 1]);
            while(i < smithereens.Length)
            {
                var shard = smithereens[i];
                if(IsLinqCall(shard))
                {
                    int j;
                    for(j = i; j < smithereens.Length; ++j)
                    {
                        if(!IsLinqCall(smithereens[j]) || IsEndOfMethodsChain((MethodCallExpression)smithereens[j]))
                            break;
                    }
                    if(j < smithereens.Length && IsLinqCall(smithereens[j]) && IsEndOfMethodsChain((MethodCallExpression)smithereens[j]))
                        ++j;
                    node = ProcessMethodsChain(finishAction, parentContext, node, smithereens, i, j);
                    i = j;
                }
                else
                {
                    switch(shard.NodeType)
                    {
                    case ExpressionType.MemberAccess:
                        node = Expression.MakeMemberAccess(node, ((MemberExpression)shard).Member);
                        break;
                    case ExpressionType.ArrayIndex:
                        node = Expression.ArrayIndex(node, Visit(((BinaryExpression)shard).Right));
                        break;
                    case ExpressionType.Call:
                        var methodCallExpression = (MethodCallExpression)shard;
                        var arguments = GetArguments(methodCallExpression).Select(Visit).ToArray();
                        if(IsLinqCall(methodCallExpression))
                        {
                            switch(methodCallExpression.Method.Name)
                            {
                            case "ToArray":
                                node = Expression.Call(node, "ToArray", null, arguments);
                                break;
                            case "ToList":
                                break;
                            default:
                                throw new NotSupportedException("Method '" + methodCallExpression.Method + "' is not supported");
                            }
                        }
                        else
                        {
                            node = methodCallExpression.Method.IsExtension()
                                       ? Expression.Call(methodCallExpression.Method, new[] {node}.Concat(arguments))
                                       : Expression.Call(node, methodCallExpression.Method, arguments);
                        }
                        break;
                    case ExpressionType.Convert:
                        node = Expression.Convert(node, shard.Type);
                        break;
                    default:
                        throw new InvalidOperationException("Node type '" + shard.NodeType + "' is not valid at this point");
                    }
                    ++i;
                }
            }
            return node;
        }

        private static IEnumerable<Expression> GetArguments(MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method.IsExtension() ? methodCallExpression.Arguments.Skip(1) : methodCallExpression.Arguments;
        }

        private static void DefaultFinishAction(Context context, ParameterExpression current, List<Expression> expressions, List<ParameterExpression> variables)
        {
            var result = context.result;
            if(result.Type.IsGenericType && result.Type.GetGenericTypeDefinition() == typeof(List<>))
                expressions.Add(Expression.Call(result, "Add", Type.EmptyTypes, current));
        }

        private Expression ProcessMethodsChain(Action<Context, ParameterExpression, List<Expression>, List<ParameterExpression>> finishAction,
                                               Context parentContext,
                                               Expression collection,
                                               Expression[] smithereens,
                                               int start, int finish)
        {
            Type resultType;
            var lastType = smithereens[finish - 1].Type;
            bool resultIsCollection;
            if(IsEndOfMethodsChain((MethodCallExpression)smithereens[finish - 1]))
            {
                resultType = lastType;
                resultIsCollection = false;
            }
            else
            {
                resultType = typeof(List<>).MakeGenericType(lastType.GetGenericArguments());
                resultIsCollection = true;
            }
            var result = CreateParameter(resultType, "result");
            var lastMethod = ((MethodCallExpression)smithereens[finish - 1]).Method;
            var found = lastMethod.Name == "First" || lastMethod.Name == "FirstOrDefault"
                        || lastMethod.Name == "Single" || lastMethod.Name == "SingleOrDefault"
                            ? CreateParameter(typeof(bool), "found")
                            : null;
            var indexesCopy = needGlobalIndexes && (lastMethod.Name == "Single" || lastMethod.Name == "SingleOrDefault")
                                  ? CreateParameter(typeof(int[]), "indexes")
                                  : null;
            var expressions = new List<Expression>();
            if(found != null)
                expressions.Add(Expression.Assign(found, Expression.Constant(false)));
            Expression defaultValue;
            if(resultIsCollection)
                defaultValue = Expression.New(resultType);
            else
            {
                switch(lastMethod.Name)
                {
                case "All":
                    defaultValue = Expression.Constant(true);
                    break;
                case "Sum":
                    defaultValue = Expression.Convert(Expression.Default(result.Type.IsNullable() ? result.Type.GetGenericArguments()[0] : result.Type), result.Type);
                    break;
                default:
                    defaultValue = Expression.Default(result.Type);
                    break;
                }
            }
            expressions.Add(Expression.Assign(result, defaultValue));
            var variables = new List<ParameterExpression> {result};
            if(found != null)
                variables.Add(found);
            if(indexesCopy != null)
                variables.Add(indexesCopy);

            var collectionKind = GetCollectionKind(collection.Type);
            var index = CreateParameter(typeof(int), "index");
            var item = CreateParameter(collection.Type.GetItemType(), "item");
            if(collectionKind == CollectionKind.Enumerable)
            {
                var getEnumeratorMethod = collection.Type.GetMethod("GetEnumerator", BindingFlags.Public | BindingFlags.Instance);
                collection = Expression.Call(collection, getEnumeratorMethod);
            }
            var array = CreateParameter(collection.Type, "array");
            variables.Add(array);
            var cycleIndexes = new List<ParameterExpression> {index};
            if(needGlobalIndexes)
                indexes.Add(index);
            else
                variables.Add(index);
            Expression condition;
            Expression init;
            Expression itemGetter;
            switch(collectionKind)
            {
            case CollectionKind.Array:
                {
                    var length = CreateParameter(typeof(int), "length");
                    variables.Add(length);
                    init = Expression.Assign(length, Expression.ArrayLength(array));
                    condition = Expression.GreaterThanOrEqual(index, length);
                    itemGetter = Expression.ArrayIndex(array, index);
                }
                break;
            case CollectionKind.String:
                {
                    var length = CreateParameter(typeof(int), "length");
                    variables.Add(length);
                    init = Expression.Assign(length, Expression.Property(array, stringLengthProperty));
                    condition = Expression.GreaterThanOrEqual(index, length);
                    itemGetter = Expression.Call(array, stringIndexerGetter, index);
                }
                break;
            case CollectionKind.List:
                var itemProperty = collection.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                var count = CreateParameter(typeof(int), "count");
                variables.Add(count);
                var countProperty = collection.Type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                init = Expression.Assign(count, Expression.Call(array, countProperty.GetGetMethod()));
                condition = Expression.GreaterThanOrEqual(index, count);
                itemGetter = Expression.Call(array, itemProperty.GetGetMethod(), index);
                break;
            case CollectionKind.Enumerable:
                init = null;
                var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);
                condition = Expression.Not(Expression.Call(array, moveNextMethod));
                var currentProperty = collection.Type.GetProperty("Current", BindingFlags.Public | BindingFlags.Instance);
                itemGetter = Expression.Call(array, currentProperty.GetGetMethod());
                break;
            default:
                throw new InvalidOperationException();
            }
            var cycleVariables = new List<ParameterExpression> {item};
            var breakLabel = Expression.Label(result.Type);
            var cycleBody = new List<Expression>
                {
                    Expression.PreIncrementAssign(index),
                    Expression.IfThen(condition, Expression.Block(Expression.Assign(index, Expression.Constant(-1)), Expression.Break(breakLabel, result))),
                    Expression.Assign(item, itemGetter)
                };
            ProcessMethodsChain(new Context
                {
                    cycleIndexes = cycleIndexes,
                    found = found,
                    indexesCopy = indexesCopy,
                    breakLabel = breakLabel,
                    result = result,
                    parentContext = parentContext
                }, smithereens, start, finish, finishAction, item, index, cycleBody, cycleVariables);
            expressions.Add(Expression.Assign(array, collection));
            if(init != null)
                expressions.Add(init);
            expressions.Add(Expression.Assign(index, Expression.Constant(-1)));
            expressions.Add(Expression.Loop(Expression.Block(cycleVariables, cycleBody), breakLabel));
            if(lastMethod.Name == "First" || lastMethod.Name == "Single")
                expressions.Add(Expression.IfThen(Expression.Not(found), Expression.Throw(Expression.New(invalidOperationExceptionConstructor, Expression.Constant("Sequence contains no mathing elements")))));
            if(indexesCopy != null)
                expressions.Add(Expression.IfThen(found, Expression.Block(cycleIndexes.Select((indeX, i) => Expression.Assign(indeX, Expression.ArrayIndex(indexesCopy, Expression.Constant(i)))))));
            expressions.Add(result);
            return Expression.Block(variables, expressions);
        }

        private ParameterExpression CreateParameter(Type type, string name)
        {
            return Expression.Parameter(type, name + "_" + (numberOfVariables++));
        }

        private void ProcessMethodsChain(Context context, Expression[] smithereens, int from, int to, Action<Context, ParameterExpression, List<Expression>, List<ParameterExpression>> finishAction, ParameterExpression current, ParameterExpression index, List<Expression> expressions, List<ParameterExpression> variables)
        {
            if(from == to)
            {
                finishAction(context, current, expressions, variables);
                return;
            }
            var methodCallExpression = (MethodCallExpression)smithereens[from];
            switch(methodCallExpression.Method.Name)
            {
            case "Select":
                {
                    // todo selector with index
                    var selector = (LambdaExpression)methodCallExpression.Arguments[1];
                    var selectorBody = new ParameterReplacer(selector.Parameters[0], current).Visit(selector.Body);
                    var selectorParameters = new List<ParameterExpression> {current};
                    if(selector.Parameters.Count == 2)
                    {
                        selectorBody = new ParameterReplacer(selector.Parameters[1], index).Visit(selectorBody);
                        selectorParameters.Add(index);
                    }
                    selector = Expression.Lambda(selectorBody, selectorParameters);
                    current = CreateParameter(selector.Body.Type, "current");
                    variables.Add(current);
                    expressions.Add(Expression.Assign(current, Visit(selector.Body)));
                    ProcessMethodsChain(context, smithereens, from + 1, to, finishAction, current, index, expressions, variables);
                }
                break;
            case "SelectMany":
                {
                    var collectionSelector = (LambdaExpression)methodCallExpression.Arguments[1];
                    collectionSelector = Expression.Lambda(new ParameterReplacer(collectionSelector.Parameters.Single(), current).Visit(collectionSelector.Body), current);

                    if(collectionSelector.Body.IsLinkOfChain(false, false))
                    {
                        var newIndex = CreateParameter(typeof(int), "index");
                        variables.Add(newIndex);
                        expressions.Add(Expression.IfThen(Expression.Equal(index, Expression.Constant(0)), Expression.Assign(newIndex, Expression.Constant(-1))));
                        var itemType = collectionSelector.Body.Type.GetItemType();
                        var parameter = Expression.Parameter(itemType);
                        var selector = Expression.Lambda(parameter, parameter);
                        expressions.Add(VisitChain((conteXt, curreNt, eXpressions, variablEs) =>
                            {
                                eXpressions.Add(Expression.PreIncrementAssign(newIndex));

                                if(methodCallExpression.Arguments.Count > 2)
                                {
                                    var resultSelector = (LambdaExpression)methodCallExpression.Arguments[2];
                                    var resultSelectorBody = new ParameterReplacer(resultSelector.Parameters[0], current).Visit(resultSelector.Body);
                                    resultSelectorBody = new ParameterReplacer(resultSelector.Parameters[1], curreNt).Visit(resultSelectorBody);
                                    resultSelector = Expression.Lambda(resultSelectorBody, current, curreNt);
                                    curreNt = CreateParameter(resultSelector.Body.Type, "current");
                                    variablEs.Add(curreNt);
                                    eXpressions.Add(Expression.Assign(curreNt, Visit(resultSelector.Body)));
                                }
                                ProcessMethodsChain(conteXt.parentContext, smithereens, from + 1, to, DefaultFinishAction, curreNt, newIndex, eXpressions, variablEs);
                            }, context, Expression.Call(selectMethod.MakeGenericMethod(itemType, itemType), collectionSelector.Body, selector)));
                    }
                    else
                    {
                        throw new NotImplementedException();
                        /*var collection = Visit(collectionSelector.Body);

                    var collectionKind = GetCollectionKind(collection.Type);
                    var array = CreateParameter(collection.Type, "array");
                    variables.Add(array);
                    var collectionIndex = CreateParameter(typeof(int), "index");
                    var item = CreateParameter(array.Type.GetItemType(), "item");
                    cycleIndexes.Add(collectionIndex);
                    if(needGlobalIndexes)
                        indexes.Add(collectionIndex);
                    else
                        variables.Add(collectionIndex);
                    Expression condition;
                    Expression init;
                    Expression itemGetter;
                    switch(collectionKind)
                    {
                    case CollectionKind.Array:
                        var length = CreateParameter(typeof(int), "length");
                        variables.Add(length);
                        init = Expression.Assign(length, Expression.ArrayLength(array));
                        condition = Expression.GreaterThanOrEqual(collectionIndex, length);
                        itemGetter = Expression.ArrayIndex(array, collectionIndex);
                        break;
                    case CollectionKind.List:
                        var itemProperty = collection.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                        var count = CreateParameter(typeof(int), "count");
                        variables.Add(count);
                        var countProperty = collection.Type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                        init = Expression.Assign(count, Expression.Call(array, countProperty.GetGetMethod()));
                        condition = Expression.GreaterThanOrEqual(collectionIndex, count);
                        itemGetter = Expression.Call(array, itemProperty.GetGetMethod(), collectionIndex);
                        break;
                    case CollectionKind.Enumerable:
                        init = null;
                        var moveNextMethod = collection.Type.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);
                        condition = Expression.Not(Expression.Call(array, moveNextMethod));
                        var currentProperty = collection.Type.GetProperty("Current", BindingFlags.Public | BindingFlags.Instance);
                        itemGetter = Expression.Call(currentProperty.GetGetMethod(), array);
                        break;
                    default:
                        throw new InvalidOperationException();
                    }
                    var newIndex = CreateParameter(typeof(int), "index");
                    variables.Add(newIndex);
                    expressions.Add(Expression.Assign(array, collection));
                    if(init != null)
                        expressions.Add(init);
                    expressions.Add(Expression.Assign(collectionIndex, Expression.Constant(-1)));
                    expressions.Add(Expression.IfThen(Expression.Equal(index, Expression.Constant(0)), Expression.Assign(newIndex, Expression.Constant(-1))));
                    var cycleVariables = new List<ParameterExpression> {item};
                    var localBreakLabel = Expression.Label();
                    var cycleBody = new List<Expression>
                        {
                            Expression.PreIncrementAssign(collectionIndex),
                            Expression.IfThen(condition, Expression.Block(Expression.Assign(collectionIndex, Expression.Constant(-1)), Expression.Break(localBreakLabel))),
                            Expression.Assign(item, itemGetter),
                            Expression.PreIncrementAssign(newIndex)
                        };

                    if(methodCallExpression.Arguments.Count == 2)
                        current = item;
                    else
                    {
                        var resultSelector = (LambdaExpression)methodCallExpression.Arguments[2];
                        var resultSelectorBody = new ParameterReplacer(resultSelector.Parameters[0], current).Visit(resultSelector.Body);
                        resultSelectorBody = new ParameterReplacer(resultSelector.Parameters[1], item).Visit(resultSelectorBody);
                        resultSelector = Expression.Lambda(resultSelectorBody, current, item);
                        current = CreateParameter(resultSelector.Body.Type, "current");
                        cycleBody.Add(Expression.Assign(current, Visit(resultSelector.Body)));
                    }

                    ProcessMethodsChain(smithereens, start + 1, finish, finishAction, current, cycleBody, cycleVariables, cycleIndexes, result, found, indexesCopy, newIndex, breakLabel);

                    expressions.Add(Expression.Loop(Expression.Block(cycleVariables, cycleBody), localBreakLabel));*/
                    }
                }
                break;
            case "Where":
                {
                    var predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                    var predicateBody = new ParameterReplacer(predicate.Parameters[0], current).Visit(predicate.Body);
                    var predicateParameters = new List<ParameterExpression> {current};
                    if(predicate.Parameters.Count == 2)
                    {
                        predicateBody = new ParameterReplacer(predicate.Parameters[1], index).Visit(predicateBody);
                        predicateParameters.Add(index);
                    }
                    predicate = Expression.Lambda(predicateBody, predicateParameters);
                    var localVariables = new List<ParameterExpression>();
                    var newIndex = CreateParameter(typeof(int), "index");
                    variables.Add(newIndex);
                    var localExpressions = new List<Expression> {Expression.PreIncrementAssign(newIndex)};
                    ProcessMethodsChain(context, smithereens, from + 1, to, finishAction, current, newIndex, localExpressions, localVariables);
                    expressions.Add(Expression.Assign(newIndex, Expression.Constant(-1)));
                    expressions.Add(Expression.IfThen(Visit(predicate.Body), Expression.Block(localVariables, localExpressions)));
                }
                break;
            case "First":
            case "FirstOrDefault":
                {
                    if(methodCallExpression.Arguments.Count == 1)
                    {
                        expressions.Add(Expression.Assign(context.result, current));
                        expressions.Add(Expression.Assign(context.found, Expression.Constant(true)));
                        expressions.Add(Expression.Break(context.breakLabel, context.result));
                    }
                    else
                    {
                        var predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                        predicate = Expression.Lambda(new ParameterReplacer(predicate.Parameters.Single(), current).Visit(predicate.Body), current);
                        expressions.Add(
                            Expression.IfThen(
                                Visit(predicate.Body),
                                Expression.Block(new List<Expression>
                                    {
                                        Expression.Assign(context.result, current),
                                        Expression.Assign(context.found, Expression.Constant(true)),
                                        Expression.Break(context.breakLabel, context.result)
                                    })));
                    }
                }
                break;
            case "Any":
                {
                    if(methodCallExpression.Arguments.Count == 1)
                    {
                        expressions.Add(Expression.Assign(context.result, Expression.Constant(true)));
                        expressions.Add(Expression.Break(context.breakLabel, context.result));
                    }
                    else
                    {
                        var predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                        predicate = Expression.Lambda(new ParameterReplacer(predicate.Parameters.Single(), current).Visit(predicate.Body), current);
                        expressions.Add(
                            Expression.IfThen(
                                Visit(predicate.Body),
                                Expression.Block(new List<Expression>
                                    {
                                        Expression.Assign(context.result, Expression.Constant(true)),
                                        Expression.Break(context.breakLabel, context.result)
                                    })));
                    }
                }
                break;
            case "All":
                {
                    var predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                    predicate = Expression.Lambda(new ParameterReplacer(predicate.Parameters.Single(), current).Visit(predicate.Body), current);
                    expressions.Add(
                        Expression.IfThen(
                            Expression.Not(Visit(predicate.Body)),
                            Expression.Block(new List<Expression>
                                {
                                    Expression.Assign(context.result, Expression.Constant(false)),
                                    Expression.Break(context.breakLabel, context.result)
                                })));
                }
                break;
            case "Sum":
                {
                    if(methodCallExpression.Arguments.Count == 1)
                        expressions.Add(Expression.AddAssign(context.result, Coalesce(current)));
                    else
                    {
                        var selector = (LambdaExpression)methodCallExpression.Arguments[1];
                        selector = Expression.Lambda(new ParameterReplacer(selector.Parameters.Single(), current).Visit(selector.Body), current);
                        expressions.Add(Expression.AddAssign(context.result, Coalesce(Visit(selector.Body))));
                    }
                }
                break;
            case "Single":
            case "SingleOrDefault":
                {
                    if(methodCallExpression.Arguments.Count == 1)
                    {
                        expressions.Add(Expression.IfThen(context.found, Expression.Throw(Expression.New(invalidOperationExceptionConstructor, Expression.Constant("Sequence contains more than one element")))));
                        expressions.Add(Expression.Assign(context.result, current));
                        expressions.Add(Expression.Assign(context.found, Expression.Constant(true)));
                        if(context.indexesCopy != null)
                            expressions.Add(Expression.Assign(context.indexesCopy, Expression.NewArrayInit(typeof(int), context.cycleIndexes)));
                    }
                    else
                    {
                        var predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                        predicate = Expression.Lambda(new ParameterReplacer(predicate.Parameters.Single(), current).Visit(predicate.Body), current);
                        expressions.Add(
                            Expression.IfThen(
                                Visit(predicate.Body),
                                Expression.Block(new List<Expression>
                                    {
                                        Expression.IfThen(context.found, Expression.Throw(Expression.New(invalidOperationExceptionConstructor, Expression.Constant("Sequence contains more than one element")))),
                                        Expression.Assign(context.result, current),
                                        Expression.Assign(context.found, Expression.Constant(true)),
                                    })));
                    }
                }
                break;
            default:
                throw new NotSupportedException("Method '" + methodCallExpression.Method + "' is not supported");
            }
        }

        private static Expression Coalesce(Expression exp)
        {
            return !exp.Type.IsNullable()
                       ? exp
                       : Expression.Convert(Expression.Call(exp, "GetValueOrDefault", Type.EmptyTypes), exp.Type);
        }

        private static bool IsEndOfMethodsChain(MethodCallExpression node)
        {
            var name = node.Method.Name;
            return name == "First" || name == "FirstOrDefault" || name == "Single" || name == "SingleOrDefault"
                   || name == "Sum" || name == "Max" || name == "Min" || name == "Average"
                   || name == "Aggregate" || name == "All" || name == "Any";
        }

        private static bool IsLinqCall(Expression node)
        {
            return node.NodeType == ExpressionType.Call && IsLinqMethod(((MethodCallExpression)node).Method);
        }

        private static bool IsLinqMethod(MethodInfo method)
        {
            if(method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();
            return method.DeclaringType == typeof(Enumerable) && method.Name != "ToArray" && method.Name != "ToList" && method.Name != "ToDictionary";
        }

        private static CollectionKind GetCollectionKind(Type type)
        {
            if(type == typeof(string))
                return CollectionKind.String;
            if(type.IsArray)
                return CollectionKind.Array;
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return CollectionKind.List;
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                return CollectionKind.List;
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return CollectionKind.Enumerable;
            var interfaces = type.GetInterfaces();
            if(interfaces.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)))
                return CollectionKind.List;
            if(interfaces.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                return CollectionKind.Enumerable;
            throw new NotSupportedException("Type '" + type + "' is not supported");
        }

        private bool needGlobalIndexes;
        private int numberOfVariables;
        private List<ParameterExpression> indexes;

        private static readonly ConstructorInfo invalidOperationExceptionConstructor = ((NewExpression)((Expression<Func<string, InvalidOperationException>>)(s => new InvalidOperationException(s))).Body).Constructor;

        private static readonly MethodInfo selectMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, IEnumerable<int>>>)(enumerable => enumerable.Select(x => x))).Body).Method.GetGenericMethodDefinition();
        private static readonly PropertyInfo stringLengthProperty = (PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member;
        private static readonly MethodInfo stringIndexerGetter = ((MethodCallExpression)((Expression<Func<string, char>>)(s => s[0])).Body).Method;

        private class Context
        {
            public ParameterExpression result;
            public List<ParameterExpression> cycleIndexes;
            public ParameterExpression found;
            public ParameterExpression indexesCopy;
            public LabelTarget breakLabel;
            public Context parentContext;
        }

        private enum CollectionKind
        {
            Array,
            String,
            List,
            Enumerable
        }
    }
}