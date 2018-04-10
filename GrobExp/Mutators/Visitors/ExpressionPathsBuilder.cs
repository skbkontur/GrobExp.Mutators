using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public static class ExpressionPathsBuilder
    {
        public static Expression BuildPaths(Expression node, ParameterExpression[] indexes)
        {
            return BuildPaths(node, indexes, new Dictionary<ParameterExpression, SinglePaths>()).ToExpression();
        }

        public static SinglePaths BuildPaths(Expression node, ParameterExpression[] indexes, Dictionary<ParameterExpression, SinglePaths> paths)
        {
            var context = new Context(indexes);
            foreach (var pair in paths)
                context.parameters.Add(pair.Key, pair.Value.Clone());
            var result = (SinglePaths)BuildPaths(node, context);
            result.MergeWith(new SinglePaths {paths = context.paths});
            return result;
        }

        private static Paths BuildPaths(IEnumerable<Expression> list, Context context)
        {
            Paths result = null;
            foreach (var exp in list)
            {
                var current = BuildPaths(exp, context);
                if (result == null)
                    result = current;
                else
                    result.MergeWith(current);
            }

            return result ?? new SinglePaths {paths = new List<List<Expression>>()};
        }

        private static Paths BuildPaths(Expression node, Context context)
        {
            if (node == null)
                return new SinglePaths {paths = new List<List<Expression>>()};
            if (node.IsLinkOfChain(true, true) && !BadNode(node))
                return BuildPathsForChain(node, context);
            switch (node.NodeType)
            {
            case ExpressionType.Add:
            case ExpressionType.AddAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.AddChecked:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.AndAssign:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Assign:
            case ExpressionType.Coalesce:
            case ExpressionType.Divide:
            case ExpressionType.DivideAssign:
            case ExpressionType.Equal:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LeftShift:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Modulo:
            case ExpressionType.ModuloAssign:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.NotEqual:
            case ExpressionType.Or:
            case ExpressionType.OrAssign:
            case ExpressionType.OrElse:
            case ExpressionType.Power:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShift:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractAssign:
            case ExpressionType.SubtractAssignChecked:
            case ExpressionType.SubtractChecked:
                return BuildPathsBinary((BinaryExpression)node, context);
            case ExpressionType.ArrayLength:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.Decrement:
            case ExpressionType.Increment:
            case ExpressionType.IsFalse:
            case ExpressionType.IsTrue:
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.OnesComplement:
            case ExpressionType.PostDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.TypeAs:
            case ExpressionType.UnaryPlus:
            case ExpressionType.Unbox:
            case ExpressionType.Quote:
            case ExpressionType.Throw:
                return BuildPathsUnary((UnaryExpression)node, context);
            case ExpressionType.Block:
                return BuildPathsBlock((BlockExpression)node, context);
            case ExpressionType.Call:
                return BuildPathsCall((MethodCallExpression)node, context);
            case ExpressionType.Conditional:
                return BuildPathsConditional((ConditionalExpression)node, context);
            case ExpressionType.Constant:
                return BuildPathsConstant((ConstantExpression)node, context);
            case ExpressionType.Parameter:
                return BuildPathsParameter((ParameterExpression)node, context);
            case ExpressionType.MemberAccess:
                return BuildPathsMemberAccess((MemberExpression)node, context);
            case ExpressionType.DebugInfo:
                return BuildPathsDebugInfo((DebugInfoExpression)node, context);
            case ExpressionType.Default:
                return BuildPathsDefault((DefaultExpression)node, context);
            case ExpressionType.Dynamic:
                return BuildPathsDynamic((DynamicExpression)node, context);
            case ExpressionType.Extension:
                return BuildPathsExtension(node, context);
            case ExpressionType.Goto:
                return BuildPathsGoto((GotoExpression)node, context);
            case ExpressionType.Index:
                return BuildPathsIndex((IndexExpression)node, context);
            case ExpressionType.Invoke:
                return BuildPathsInvoke((InvocationExpression)node, context);
            case ExpressionType.Label:
                return BuildPathsLabel((LabelExpression)node, context);
            case ExpressionType.Lambda:
                return BuildPathsLambda((LambdaExpression)node, context);
            case ExpressionType.ListInit:
                return BuildPathsListInit((ListInitExpression)node, context);
            case ExpressionType.Loop:
                return BuildPathsLoop((LoopExpression)node, context);
            case ExpressionType.MemberInit:
                return BuildPathsMemberInit((MemberInitExpression)node, context);
            case ExpressionType.New:
                return BuildPathsNew((NewExpression)node, context);
            case ExpressionType.NewArrayBounds:
            case ExpressionType.NewArrayInit:
                return BuildPathsNewArray((NewArrayExpression)node, context);
            case ExpressionType.RuntimeVariables:
                return BuildPathsRuntimeVariables((RuntimeVariablesExpression)node, context);
            case ExpressionType.Switch:
                return BuildPathsSwitch((SwitchExpression)node, context);
            case ExpressionType.Try:
                return BuildPathsTry((TryExpression)node, context);
            case ExpressionType.TypeEqual:
            case ExpressionType.TypeIs:
                return BuildPathsTypeBinary((TypeBinaryExpression)node, context);
            default:
                throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
            }
        }

        private static Paths BuildPathsForChain(Expression node, Context context)
        {
            var smithereens = node.SmashToSmithereens();
            Paths paths;
            paths = context.parameters.TryGetValue((ParameterExpression)smithereens[0], out paths)
                        ? paths.Clone()
                        : new SinglePaths {paths = new List<List<Expression>> {new List<Expression>()}};
            for (var i = 1; i < smithereens.Length; ++i)
            {
                var shard = smithereens[i];
                if (IsLinqCall(shard))
                {
                    var methodCallExpression = (MethodCallExpression)shard;
                    var method = methodCallExpression.Method;

                    if (!IsLinqCall(smithereens[i - 1]) || IsEndOfLinqChain(smithereens[i - 1]))
                    {
                        paths.Add(context.indexes[context.index]);
                        context.index++;
                    }

                    if (method.IsEachMethod())
                    {
                    }
                    else
                    {
                        switch (method.Name)
                        {
                        case "First":
                        case "FirstOrDefault":
                        case "Single":
                        case "SingleOrDefault":
                        case "Any":
                        case "All":
                            break;
                        case "Where":
                            {
                                var predicate = (LambdaExpression)methodCallExpression.Arguments[1];
                                var subContext = new Context(context.indexes) {index = context.index};
                                BuildPaths(predicate.Body, subContext);
                                context.index = subContext.index;
                            }
                            break;
                        case "Select":
                            {
                                var selector = (LambdaExpression)methodCallExpression.Arguments[1];
                                var parameter = selector.Parameters.Single();
                                context.parameters.Add(parameter, paths.Clone());
                                paths = BuildPaths(selector.Body, context);
                                context.parameters.Remove(parameter);
                            }
                            break;
                        case "SelectMany":
                            {
                                var collectionSelector = (LambdaExpression)methodCallExpression.Arguments[1];
                                var parameter = collectionSelector.Parameters.Single();
                                var startPaths = paths.Clone();
                                context.parameters.Add(parameter, paths.Clone());
                                paths = BuildPaths(collectionSelector.Body, context);
                                context.parameters.Remove(parameter);
                                if (!IsLinqCall(collectionSelector.Body))
                                {
                                    paths.Add(context.indexes[context.index]);
                                    context.index++;
                                }

                                if (methodCallExpression.Arguments.Count > 2)
                                {
                                    var resultSelector = (LambdaExpression)methodCallExpression.Arguments[2];
                                    context.parameters.Add(resultSelector.Parameters[0], startPaths);
                                    context.parameters.Add(resultSelector.Parameters[1], paths.Clone());
                                    paths = BuildPaths(resultSelector.Body, context);
                                    context.parameters.Remove(resultSelector.Parameters[1]);
                                    context.parameters.Remove(resultSelector.Parameters[0]);
                                }
                            }
                            break;
                        default:
                            throw new NotSupportedException("Method '" + method + "' is not supported");
                        }
                    }
                }
                else
                {
                    switch (shard.NodeType)
                    {
                    case ExpressionType.MemberAccess:
                        paths = AddMember(paths, ((MemberExpression)shard).Member);
                        break;
                    case ExpressionType.ArrayIndex:
                        paths.Add(((BinaryExpression)shard).Right);
                        break;
                    case ExpressionType.Call:
                        var methodCallExpression = (MethodCallExpression)shard;
                        if (!methodCallExpression.Method.IsIndexerGetter())
                            throw new InvalidOperationException("Method '" + methodCallExpression.Method + "' is not valid at this point");
                        foreach (var argument in methodCallExpression.Arguments)
                            paths.Add(argument);
                        break;
                    case ExpressionType.Convert:
                        break;
                    default:
                        throw new InvalidOperationException("Node type '" + shard.NodeType + "' is not valid at this point");
                    }
                }
            }

            return paths;
        }

        private static bool IsLinqCall(Expression node)
        {
            return node.NodeType == ExpressionType.Call && IsLinqMethod(((MethodCallExpression)node).Method);
        }

        private static bool IsEndOfLinqChain(Expression node)
        {
            return node.NodeType == ExpressionType.Call && IsEndOfLinqChain(((MethodCallExpression)node).Method);
        }

        private static bool IsLinqMethod(MethodInfo method)
        {
            if (method.IsEachMethod())
                return true;
            if (method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();
            return method.DeclaringType == typeof(Enumerable);
        }

        private static bool IsEndOfLinqChain(MethodInfo method)
        {
            if (method.IsEachMethod())
                return true;
            if (method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();
            return method.DeclaringType == typeof(Enumerable)
                   && (method.Name == "First" || method.Name == "FirstOrDefault"
                                              || method.Name == "Single" || method.Name == "SingleOrDefault"
                                              || method.Name == "Aggregate");
        }

        private static bool BadNode(Expression node)
        {
            if (node.NodeType != ExpressionType.Call)
                return false;
            var method = ((MethodCallExpression)node).Method;
            if (!IsLinqMethod(method))
                return false;
            return method.Name == "ToArray" || method.Name == "ToList" || method.Name == "ToDictionary";
        }

        private static Paths BuildPathsUnary(UnaryExpression node, Context context)
        {
            return BuildPaths(node.Operand, context);
        }

        private static Paths BuildPathsBinary(BinaryExpression node, Context context)
        {
            var result = BuildPaths(node.Left, context);
            result.MergeWith(BuildPaths(node.Right, context));
            return result;
        }

        private static Paths BuildPathsBlock(BlockExpression node, Context context)
        {
            return BuildPaths(node.Expressions, context);
        }

        private static Paths BuildPathsCall(MethodCallExpression node, Context context)
        {
            var result = BuildPaths(node.Object, context);
            result.MergeWith(BuildPaths(node.Arguments, context));
            return result;
        }

        private static Paths BuildPathsConditional(ConditionalExpression node, Context context)
        {
            BuildPaths(node.Test, context);
            var result = BuildPaths(node.IfTrue, context);
            result.MergeWith(BuildPaths(node.IfFalse, context));
            return result;
        }

        private static Paths BuildPathsConstant(ConstantExpression node, Context context)
        {
            return new SinglePaths {paths = new List<List<Expression>>()};
        }

        private static Paths BuildPathsParameter(ParameterExpression node, Context context)
        {
            Paths result;
            if (!context.parameters.TryGetValue(node, out result))
                result = new SinglePaths {paths = new List<List<Expression>>()};
            return result;
        }

        private static Paths AddMember(Paths paths, MemberInfo member)
        {
            var multiplePaths = paths as MultiplePaths;
            if (multiplePaths != null)
            {
                List<List<Expression>> list;
                if (multiplePaths.paths.TryGetValue(member, out list))
                    return new SinglePaths {paths = list};
            }

            paths.Add(Expression.Constant(member.Name));
            return paths;
        }

        private static Paths BuildPathsMemberAccess(MemberExpression node, Context context)
        {
            return AddMember(BuildPaths(node.Expression, context), node.Member);
        }

        private static Paths BuildPathsDebugInfo(DebugInfoExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsDefault(DefaultExpression node, Context context)
        {
            return new SinglePaths {paths = new List<List<Expression>>()};
        }

        private static Paths BuildPathsDynamic(DynamicExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsExtension(Expression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsGoto(GotoExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsIndex(IndexExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsInvoke(InvocationExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsLabel(LabelExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsLambda(LambdaExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsListInit(ListInitExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsLoop(LoopExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsMemberInit(MemberInitExpression node, Context context)
        {
            var result = BuildPaths(node.NewExpression, context);
            foreach (MemberAssignment binding in node.Bindings)
                result.MergeWith(BuildPaths(binding.Expression, context));
            return result;
        }

        private static Paths BuildPathsNew(NewExpression node, Context context)
        {
            if (node.Type.IsAnonymousType())
            {
                var paths = new Dictionary<MemberInfo, List<List<Expression>>>();
                var properties = node.Type.GetProperties();
                for (var i = 0; i < properties.Length; ++i)
                {
                    var arg = BuildPaths(node.Arguments[i], context);
                    var singlePath = arg as SinglePaths;
                    if (singlePath == null)
                        throw new NotSupportedException();
                    paths.Add(properties[i], singlePath.paths);
                }

                return new MultiplePaths {paths = paths};
            }

            return BuildPaths(node.Arguments, context);
        }

        private static Paths BuildPathsNewArray(NewArrayExpression node, Context context)
        {
            return BuildPaths(node.Expressions, context);
        }

        private static Paths BuildPathsRuntimeVariables(RuntimeVariablesExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsSwitch(SwitchExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsTry(TryExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static Paths BuildPathsTypeBinary(TypeBinaryExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        public abstract class Paths
        {
            public abstract void Add(Expression piece);
            public abstract void MergeWith(Paths other);
            public abstract Paths Clone();
        }

        public class SinglePaths : Paths
        {
            public override void Add(Expression piece)
            {
                foreach (var path in paths)
                    path.Add(piece);
            }

            public override void MergeWith(Paths other)
            {
                var singlePaths = other as SinglePaths;
                if (singlePaths == null)
                    throw new InvalidOperationException();
                paths.AddRange(singlePaths.paths);
            }

            public override Paths Clone()
            {
                return new SinglePaths {paths = paths.Select(list => new List<Expression>(list)).ToList()};
            }

            public Expression ToExpression()
            {
                return Expression.NewArrayInit(typeof(string[]), from path in paths
                                                                 select Expression.NewArrayInit(typeof(string),
                                                                                                from piece in path
                                                                                                select piece.Type == typeof(string)
                                                                                                           ? piece
                                                                                                           : Expression.Call(piece, "ToString", Type.EmptyTypes)));
            }

            public List<List<Expression>> paths;
        }

        public class MultiplePaths : Paths
        {
            public override void Add(Expression piece)
            {
                foreach (var path in paths.Values.SelectMany(path => path))
                    path.Add(piece);
            }

            public override void MergeWith(Paths other)
            {
                var multiplePaths = other as MultiplePaths;
                if (multiplePaths == null)
                    throw new InvalidOperationException();
                foreach (var pair in multiplePaths.paths)
                {
                    List<List<Expression>> list;
                    if (!paths.TryGetValue(pair.Key, out list))
                        paths.Add(pair.Key, list = new List<List<Expression>>());
                    list.AddRange(pair.Value);
                }
            }

            public override Paths Clone()
            {
                return new MultiplePaths {paths = paths.ToDictionary(pair => pair.Key, pair => pair.Value.Select(list => new List<Expression>(list)).ToList())};
            }

            public Dictionary<MemberInfo, List<List<Expression>>> paths;
        }

        private class Context
        {
            public Context(ParameterExpression[] indexes)
            {
                this.indexes = indexes;
            }

            public readonly ParameterExpression[] indexes;
            public readonly List<List<Expression>> paths = new List<List<Expression>>();
            public int index;
            public readonly Dictionary<ParameterExpression, Paths> parameters = new Dictionary<ParameterExpression, Paths>();
        }
    }
}