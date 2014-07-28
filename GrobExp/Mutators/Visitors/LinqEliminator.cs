using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public class LinqEliminator : ExpressionVisitor
    {
        public Expression Eliminate(Expression node)
        {
            needGlobalIndexes = false;
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

        private bool needGlobalIndexes;
        private List<ParameterExpression> indexes;

        public override Expression Visit(Expression node)
        {
            return node.IsLinkOfChain(false, false) ? VisitChain(node) : base.Visit(node);
        }

        private Expression VisitChain(Expression node)
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
                    if(j < smithereens.Length && IsEndOfMethodsChain((MethodCallExpression)smithereens[j]))
                        ++j;
                    node = ProcessMethodsChain(node, smithereens, i, j);
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
                        node = methodCallExpression.Method.IsExtension()
                                   ? Expression.Call(methodCallExpression.Method, new[] {node}.Concat(arguments))
                                   : Expression.Call(node, methodCallExpression.Method, arguments);
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

        private Expression ProcessMethodsChain(Expression collection, Expression[] smithereens, int from, int to)
        {
            var result = Expression.Parameter(smithereens[to - 1].Type, "result");
            var lastMethod = ((MethodCallExpression)smithereens[to - 1]).Method;
            var found = NeedFound(lastMethod) ? Expression.Parameter(typeof(bool), "found") : null;
            var expressions = new List<Expression>();
            if(found != null)
                expressions.Add(Expression.Assign(found, Expression.Constant(false)));
            expressions.Add(Expression.Assign(result, Expression.Default(result.Type)));
            var variables = new List<ParameterExpression> {result};
            if(found != null)
                variables.Add(found);

            var collectionKind = GetCollectionKind(collection.Type);
            var array = Expression.Parameter(collection.Type, "array");
            variables.Add(array);
            var index = Expression.Parameter(typeof(int), "index");
            var item = Expression.Parameter(array.Type.GetItemType(), "item");
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
                var length = Expression.Parameter(typeof(int), "length");
                variables.Add(length);
                init = Expression.Assign(length, Expression.ArrayLength(array));
                condition = Expression.GreaterThanOrEqual(index, length);
                itemGetter = Expression.ArrayIndex(array, index);
                break;
            case CollectionKind.List:
                var itemProperty = collection.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                var count = Expression.Parameter(typeof(int), "count");
                variables.Add(count);
                var countProperty = collection.Type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                init = Expression.Assign(count, Expression.Call(array, countProperty.GetGetMethod()));
                condition = Expression.GreaterThanOrEqual(index, count);
                itemGetter = Expression.Call(array, itemProperty.GetGetMethod(), index);
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
            var cycleVariables = new List<ParameterExpression> {item};
            var breakLabel = Expression.Label(result.Type);
            var cycleBody = new List<Expression>
                {
                    Expression.PreIncrementAssign(index),
                    Expression.IfThen(condition, Expression.Block(Expression.Assign(index, Expression.Constant(-1)), Expression.Break(breakLabel, result))),
                    Expression.Assign(item, itemGetter)
                };
            ProcessMethodsChain(smithereens, from, to, item, cycleBody, cycleVariables, result, found, index, breakLabel);
            expressions.Add(Expression.Assign(array, collection));
            if(init != null)
                expressions.Add(init);
            expressions.Add(Expression.Assign(index, Expression.Constant(-1)));
            expressions.Add(Expression.Loop(Expression.Block(cycleVariables, cycleBody), breakLabel));
            if(lastMethod.Name == "First" || lastMethod.Name == "Single")
            {
                expressions.Add(Expression.IfThen(Expression.Not(found), Expression.Throw(Expression.New(invalidOperationExceptionConstructor, Expression.Constant("Sequence contains no mathing elements")))));
                expressions.Add(result);
            }
            return Expression.Block(variables, expressions);
        }

        private void ProcessMethodsChain(
            Expression[] smithereens, int from, int to,
            ParameterExpression current, List<Expression> expressions, List<ParameterExpression> variables,
            ParameterExpression result, ParameterExpression found, ParameterExpression index, LabelTarget breakLabel)
        {
            if(from == to)
                return;
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
                    current = Expression.Parameter(selector.Body.Type);
                    variables.Add(current);
                    expressions.Add(Expression.Assign(current, Visit(selector.Body)));
                    ProcessMethodsChain(smithereens, from + 1, to, current, expressions, variables, result, found, index, breakLabel);
                }
                break;
            case "SelectMany":
                {
                    var collectionSelector = (LambdaExpression)methodCallExpression.Arguments[1];
                    collectionSelector = Expression.Lambda(new ParameterReplacer(collectionSelector.Parameters.Single(), current).Visit(collectionSelector.Body), current);
                    var collection = Visit(collectionSelector.Body);

                    var collectionKind = GetCollectionKind(collection.Type);
                    var array = Expression.Parameter(collection.Type, "array");
                    variables.Add(array);
                    var collectionIndex = Expression.Parameter(typeof(int), "index");
                    var item = Expression.Parameter(array.Type.GetItemType(), "item");
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
                        var length = Expression.Parameter(typeof(int), "length");
                        variables.Add(length);
                        init = Expression.Assign(length, Expression.ArrayLength(array));
                        condition = Expression.GreaterThanOrEqual(collectionIndex, length);
                        itemGetter = Expression.ArrayIndex(array, collectionIndex);
                        break;
                    case CollectionKind.List:
                        var itemProperty = collection.Type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                        var count = Expression.Parameter(typeof(int), "count");
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
                    var newIndex = Expression.Parameter(typeof(int), "index");
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
                        current = Expression.Parameter(resultSelector.Body.Type);
                        cycleBody.Add(Expression.Assign(current, Visit(resultSelector.Body)));
                    }

                    ProcessMethodsChain(smithereens, from + 1, to, current, cycleBody, cycleVariables, result, found, newIndex, breakLabel);

                    expressions.Add(Expression.Loop(Expression.Block(cycleVariables, cycleBody), localBreakLabel));
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
                    var newIndex = Expression.Parameter(typeof(int), "index");
                    variables.Add(newIndex);
                    var localExpressions = new List<Expression> {Expression.PreIncrementAssign(newIndex)};
                    ProcessMethodsChain(smithereens, from + 1, to, current, localExpressions, localVariables, result, found, newIndex, breakLabel);
                    expressions.Add(Expression.Assign(newIndex, Expression.Constant(-1)));
                    expressions.Add(Expression.IfThen(Visit(predicate.Body), Expression.Block(localVariables, localExpressions)));
                }
                break;
            case "First":
            case "FirstOrDefault":
                {
                    if(methodCallExpression.Arguments.Count == 1)
                    {
                        expressions.Add(Expression.Assign(result, current));
                        expressions.Add(Expression.Assign(found, Expression.Constant(true)));
                        expressions.Add(Expression.Break(breakLabel, result));
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
                                        Expression.Assign(result, current),
                                        Expression.Assign(found, Expression.Constant(true)),
                                        Expression.Break(breakLabel, result)
                                    })));
                    }
                }
                break;
            case "Single":
            case "SingleOrDefault":
                {
                    if(methodCallExpression.Arguments.Count == 1)
                    {
                        expressions.Add(Expression.IfThen(found, Expression.Throw(Expression.New(invalidOperationExceptionConstructor, Expression.Constant("Sequence contains more than one element")))));
                        expressions.Add(Expression.Assign(result, current));
                        expressions.Add(Expression.Assign(found, Expression.Constant(true)));
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
                                        Expression.IfThen(found, Expression.Throw(Expression.New(invalidOperationExceptionConstructor, Expression.Constant("Sequence contains more than one element")))),
                                        Expression.Assign(result, current),
                                        Expression.Assign(found, Expression.Constant(true)),
                                    })));
                    }
                }
                break;
            default:
                throw new NotSupportedException("Method '" + methodCallExpression.Method + "' is not supported");
            }
        }

        private static bool NeedFound(MethodInfo method)
        {
            return method.Name == "First" || method.Name == "FirstOrDefault" || method.Name == "Single" || method.Name == "SingleOrDefault";
        }

        private static bool IsEndOfMethodsChain(MethodCallExpression node)
        {
            string name = node.Method.Name;
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
            return method.DeclaringType == typeof(Enumerable);
        }

        private CollectionKind GetCollectionKind(Type type)
        {
            if(type.IsArray)
                return CollectionKind.Array;
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return CollectionKind.Enumerable;
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return CollectionKind.List;
            var interfaces = type.GetInterfaces();
            if(interfaces.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)))
                return CollectionKind.List;
            if(interfaces.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                return CollectionKind.List;
            throw new NotSupportedException("Type '" + type + "' is not supported");
        }

        private static readonly ConstructorInfo invalidOperationExceptionConstructor = ((NewExpression)((Expression<Func<string, InvalidOperationException>>)(s => new InvalidOperationException(s))).Body).Constructor;

        private enum CollectionKind
        {
            Array,
            List,
            Enumerable
        }
    }
}