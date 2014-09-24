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
            var context = new Context(indexes);
            var paths = BuildPaths(node, context);
            paths.AddRange(context.paths);
            var result = new List<Expression>();
            foreach(var path in paths)
            {
                var pieces = new List<Expression>();
                foreach(var piece in path)
                    pieces.Add(Expression.Call(piece, "ToString", Type.EmptyTypes));
                result.Add(Expression.NewArrayInit(typeof(string), pieces));
            }
            return Expression.NewArrayInit(typeof(string[]), result);
        }

        private static List<List<Expression>> BuildPaths(IEnumerable<Expression> list, Context context)
        {
            var result = new List<List<Expression>>();
            foreach(var exp in list)
                result.AddRange(BuildPaths(exp, context));
            return result;
        }

        private static List<List<Expression>> BuildPaths(Expression node, Context context)
        {
            if(node == null)
                return new List<List<Expression>>();
            if(node.IsLinkOfChain(true, true))
                return BuildPathsForChain(node, context);
            switch(node.NodeType)
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

        private static List<List<Expression>> BuildPathsForChain(Expression node, Context context)
        {
            var smithereens = node.SmashToSmithereens();
            List<List<Expression>> paths;
            if(!context.parameters.TryGetValue((ParameterExpression)smithereens[0], out paths))
                paths = new List<List<Expression>>{new List<Expression>()};
            for(int i = 1; i < smithereens.Length; ++i)
            {
                var shard = smithereens[i];
                if(IsLinqCall(shard))
                {
                    var methodCallExpression = (MethodCallExpression)shard;
                    var method = methodCallExpression.Method;
                    if(!IsLinqCall(smithereens[i - 1]))
                    {
                        foreach(var path in paths)
                            path.Add(context.indexes[context.index]);
                        context.index++;
                    }
                    switch(method.Name)
                    {
                    case "First":
                    case "FirstOrDefault":
                    case "Single":
                    case "SingleOrDefault":
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
                            context.parameters.Add(parameter, paths);
                            paths = BuildPaths(selector.Body, context);
                            context.parameters.Remove(parameter);
                        }
                        break;
                    case "SelectMany":
                        {
                            var collectionSelector = (LambdaExpression)methodCallExpression.Arguments[1];
                            var parameter = collectionSelector.Parameters.Single();
                            var startPaths = paths;
                            context.parameters.Add(parameter, paths);
                            paths = BuildPaths(collectionSelector.Body, context);
                            context.parameters.Remove(parameter);
                            if(!IsLinqCall(collectionSelector.Body))
                            {
                                foreach(var path in paths)
                                    path.Add(context.indexes[context.index]);
                                context.index++;
                            }
                            if(methodCallExpression.Arguments.Count > 2)
                            {
                                var resultSelector = (LambdaExpression)methodCallExpression.Arguments[2];
                                context.parameters.Add(resultSelector.Parameters[0], startPaths);
                                context.parameters.Add(resultSelector.Parameters[1], paths);
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
                else
                {
                    switch(shard.NodeType)
                    {
                    case ExpressionType.MemberAccess:
                        foreach(var path in paths)
                            path.Add(Expression.Constant(((MemberExpression)shard).Member.Name));
                        break;
                    case ExpressionType.ArrayIndex:
                        foreach(var path in paths)
                            path.Add(((BinaryExpression)shard).Right);
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
        
        private static bool IsLinqMethod(MethodInfo method)
        {
            if(method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();
            return method.DeclaringType == typeof(Enumerable);
        }

        private static List<List<Expression>> BuildPathsUnary(UnaryExpression node, Context context)
        {
            return BuildPaths(node.Operand, context);
        }

        private static List<List<Expression>> BuildPathsBinary(BinaryExpression node, Context context)
        {
            var result = BuildPaths(node.Left, context);
            result.AddRange(BuildPaths(node.Right, context));
            return result;
        }

        private static List<List<Expression>> BuildPathsBlock(BlockExpression node, Context context)
        {
            return BuildPaths(node.Expressions, context);
        }

        private static List<List<Expression>> BuildPathsCall(MethodCallExpression node, Context context)
        {
            var result = BuildPaths(node.Object, context);
            result.AddRange(BuildPaths(node.Arguments, context));
            return result;
        }

        private static List<List<Expression>> BuildPathsConditional(ConditionalExpression node, Context context)
        {
            BuildPaths(node.Test, context);
            var result = BuildPaths(node.IfTrue, context);
            result.AddRange(BuildPaths(node.IfFalse, context));
            return result;
        }

        private static List<List<Expression>> BuildPathsConstant(ConstantExpression node, Context context)
        {
            return new List<List<Expression>>();
        }

        private static List<List<Expression>> BuildPathsParameter(ParameterExpression node, Context context)
        {
            List<List<Expression>> result;
            if(!context.parameters.TryGetValue(node, out result))
                result = new List<List<Expression>>();
            return result;
        }

        private static List<List<Expression>> BuildPathsMemberAccess(MemberExpression node, Context context)
        {
            var result = BuildPaths(node.Expression, context);
            foreach(var path in result)
                path.Add(Expression.Constant(node.Member.Name));
            return result;
        }

        private static List<List<Expression>> BuildPathsDebugInfo(DebugInfoExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsDefault(DefaultExpression node, Context context)
        {
            return new List<List<Expression>>();
        }

        private static List<List<Expression>> BuildPathsDynamic(DynamicExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsExtension(Expression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsGoto(GotoExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsIndex(IndexExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsInvoke(InvocationExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsLabel(LabelExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsLambda(LambdaExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsListInit(ListInitExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsLoop(LoopExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsMemberInit(MemberInitExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsNew(NewExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsNewArray(NewArrayExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsRuntimeVariables(RuntimeVariablesExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsSwitch(SwitchExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsTry(TryExpression node, Context context)
        {
            throw new NotImplementedException();
        }

        private static List<List<Expression>> BuildPathsTypeBinary(TypeBinaryExpression node, Context context)
        {
            throw new NotImplementedException();
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
            public readonly Dictionary<ParameterExpression, List<List<Expression>>> parameters = new Dictionary<ParameterExpression, List<List<Expression>>>();
        }
    }
}