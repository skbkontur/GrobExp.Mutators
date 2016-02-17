using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    internal static class DependenciesExtractorHelper
    {
        public static T ExternalCurrent<T>(this IEnumerable<T> arr)
        {
            throw new InvalidOperationException("Method " + ExternalCurrentMethod + " cannot be invoked");
        }

        public static readonly MethodInfo ExternalCurrentMethod = ((MethodCallExpression)((Expression<Func<int[], int>>)(arr => ExternalCurrent(arr))).Body).Method.GetGenericMethodDefinition();
    }

    public class DependenciesExtractor : ExpressionVisitor
    {
        public DependenciesExtractor(LambdaExpression lambda, IEnumerable<ParameterExpression> parameters)
        {
            this.lambda = lambda;
            this.parameters = new HashSet<ParameterExpression>(parameters);
            methodProcessors = new Dictionary<MethodInfo, MethodProcessor>
                {
                    {DependenciesExtractorHelper.ExternalCurrentMethod, ProcessExternalCurrent},
                    {MutatorsHelperFunctions.CurrentIndexMethod, ProcessCurrentIndex}
                };
        }

        public override Expression Visit(Expression node)
        {
            return node.IsLinkOfChain(true, true) ? VisitChain(node) : base.Visit(node);
        }

        public LambdaExpression[] Extract(out LambdaExpression[] primaryDependencies, out LambdaExpression[] additionalDependencies)
        {
            var bodyWithoutCurrent = new MethodReplacer(MutatorsHelperFunctions.CurrentMethod, DependenciesExtractorHelper.ExternalCurrentMethod).Visit(lambda.Body);
            var bodyWithoutEach = new MethodReplacer(MutatorsHelperFunctions.EachMethod, DependenciesExtractorHelper.ExternalCurrentMethod).Visit(bodyWithoutCurrent);
            var body = Visit(bodyWithoutEach);
            var replacer = new MethodReplacer(DependenciesExtractorHelper.ExternalCurrentMethod, MutatorsHelperFunctions.CurrentMethod);
            if(body == null)
                primaryDependencies = new LambdaExpression[0];
            else
            {
                var primaryDependenciez = new List<LambdaExpression>();
                foreach(var dependency in ((NewArrayExpression)body).Expressions.Select(ClearConverts))
                {
                    if((dependency is NewExpression))
                        primaryDependenciez.Add(MakeLambda(dependency));
                    else
                    {
                        var root = dependency.SmashToSmithereens()[0];
                        if(root.NodeType == ExpressionType.Parameter && parameters.Contains((ParameterExpression)root))
                            primaryDependenciez.Add(MakeLambda(dependency));
                    }
                }
                primaryDependencies = primaryDependenciez.Select(replacer.Visit).Cast<LambdaExpression>()
                                                         .GroupBy(exp => ExpressionCompiler.DebugViewGetter(exp)).Select(grouping => grouping.First()).ToArray();
            }
            additionalDependencies = dependencies.Select(dependency => Expression.Lambda(ClearConverts(dependency.Body), dependency.Parameters)).Where(dependency =>
                {
                    var root = dependency.Body.SmashToSmithereens()[0];
                    return root.NodeType == ExpressionType.Parameter && parameters.Contains((ParameterExpression)root);
                }).Select(replacer.Visit).Cast<LambdaExpression>().GroupBy(exp => ExpressionCompiler.DebugViewGetter(exp)).Select(grouping => grouping.First()).ToArray();
            var result = new List<LambdaExpression>(additionalDependencies);
            foreach(var primaryDependency in primaryDependencies)
                Extract(primaryDependency.Body, result);
            return result.GroupBy(exp => ExpressionCompiler.DebugViewGetter(exp)).Select(grouping => grouping.First()).ToArray();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);
            IEnumerable<Expression> expressions = new Expression[0];
            if(left != null)
                expressions = expressions.Concat(((NewArrayExpression)left).Expressions);
            if(right != null)
                expressions = expressions.Concat(((NewArrayExpression)right).Expressions);
            return Expression.NewArrayInit(typeof(object), expressions);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            return Expression.NewArrayInit(typeof(object),
                                           (from exp in node.Expressions
                                            from dependency in ((NewArrayExpression)Visit(exp)).Expressions
                                            select dependency));
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var test = Visit(node.Test);
            var ifTrue = Visit(node.IfTrue);
            var ifFalse = Visit(node.IfFalse);
            if(test != null)
            {
                foreach(var dependency in ((NewArrayExpression)test).Expressions)
                    dependencies.Add(MakeLambda(dependency));
            }
            IEnumerable<Expression> expressions = new Expression[0];
            if(ifTrue != null)
                expressions = expressions.Concat(((NewArrayExpression)ifTrue).Expressions);
            if(ifFalse != null)
                expressions = expressions.Concat(((NewArrayExpression)ifFalse).Expressions);
            return Expression.NewArrayInit(typeof(object), expressions);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            return Expression.NewArrayInit(typeof(object));
        }

        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitDynamic(DynamicExpression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitDefault(DefaultExpression node)
        {
            return Expression.NewArrayInit(typeof(object));
        }

        protected override Expression VisitExtension(Expression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitGoto(GotoExpression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            var primaryDependencies = new List<Expression>();
            foreach(var argument in node.Arguments)
            {
                var arg = Visit(argument);
                if(arg != null)
                    primaryDependencies.AddRange(((NewArrayExpression)arg).Expressions);
            }
            return Expression.NewArrayInit(typeof(object), primaryDependencies);
        }

        protected override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitLabel(LabelExpression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitLoop(LoopExpression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return Visit(node.Expression);
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var primaryDependencies = new List<Expression>();
            var o = Visit(node.Object);
            if(o != null)
                primaryDependencies.AddRange(((NewArrayExpression)o).Expressions);
            foreach(var argument in node.Arguments)
            {
                var arg = Visit(argument);
                if(arg != null)
                    primaryDependencies.AddRange(((NewArrayExpression)arg).Expressions);
            }
            return Expression.NewArrayInit(typeof(object), primaryDependencies);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            var primaryDependencies = new List<Expression>();
            foreach(var expression in node.Expressions)
            {
                var exp = Visit(expression);
                if(exp != null)
                    primaryDependencies.AddRange(((NewArrayExpression)exp).Expressions);
            }
            return Expression.NewArrayInit(typeof(object), primaryDependencies);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            var arguments = (from argument in node.Arguments
                             let arg = Visit(argument)
                             select arg == null ? null : Expression.Convert(Expression.Convert(arg, typeof(object)), argument.Type));
            return Expression.NewArrayInit(typeof(object), Expression.Convert(Expression.New(node.Constructor, arguments), typeof(object)));
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            throw new InvalidOperationException();
        }

        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            throw new NotSupportedException();
        }

        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitSwitch(SwitchExpression node)
        {
            throw new NotSupportedException();
        }

        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitTry(TryExpression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            throw new NotSupportedException();
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            return Visit(node.Operand);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var primaryDependencies = new List<Expression>();
            foreach(MemberAssignment assignment in node.Bindings)
            {
                var arg = Visit(assignment.Expression);
                if(arg != null)
                    primaryDependencies.AddRange(((NewArrayExpression)arg).Expressions);
            }
            return Expression.NewArrayInit(typeof(object), primaryDependencies);
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            throw new NotSupportedException();
        }

        protected override ElementInit VisitElementInit(ElementInit node)
        {
            throw new NotSupportedException();
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            throw new NotSupportedException();
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            throw new NotSupportedException();
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            throw new NotSupportedException();
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            throw new NotSupportedException();
        }

        private delegate void MethodProcessor(MethodInfo method, CurrentDependencies prefix, Expression[] arguments);

        private LambdaExpression MakeLambda(Expression path)
        {
            return Expression.Lambda(path, GetParameter(path));
        }

        private IEnumerable<ParameterExpression> GetParameter(Expression path)
        {
//            while(path.NodeType != ExpressionType.Parameter)
//            {
//                switch(path.NodeType)
//                {
//                case ExpressionType.MemberAccess:
//                    path = ((MemberExpression)path).Expression;
//                    break;
//                case ExpressionType.ArrayIndex:
//                    path = ((BinaryExpression)path).Left;
//                    break;
//                case ExpressionType.Call:
//                    {
//                        var methodCallExpression = (MethodCallExpression)path;
//                        if(!methodCallExpression.Method.IsEachMethod())
//                            throw new NotSupportedException("Method " + methodCallExpression.Method + " is not supported");
//                        path = methodCallExpression.Arguments.Single();
//                        break;
//                    }
//                default:
//                    throw new NotSupportedException("Node type " + path.NodeType + " is not supported");
//                }
//            }
//            return (ParameterExpression)path;
            return lambda.Parameters; //path.ExtractParameters();
        }

        private void Extract(Expression dependency, List<LambdaExpression> result)
        {
            if(dependency.NodeType == ExpressionType.New)
            {
                foreach(var argument in ((NewExpression)dependency).Arguments)
                    Extract(ClearConverts(argument), result);
            }
            else if(dependency.NodeType == ExpressionType.NewArrayInit)
            {
                foreach(var expression in ((NewArrayExpression)dependency).Expressions)
                    Extract(ClearConverts(expression), result);
            }
            else result.Add(MakeLambda(dependency));
        }

        private static Expression ClearConverts(Expression node)
        {
            while(node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
                node = ((UnaryExpression)node).Operand;
            return node;
        }

        private Expression VisitChain(Expression node)
        {
            var smithereens = node.SmashToSmithereens();
            if(smithereens.Last().IsStringLengthPropertyAccess())
                smithereens = smithereens.Take(smithereens.Length - 1).ToArray();
            if(smithereens[0].NodeType == ExpressionType.Constant)
                return Expression.NewArrayInit(typeof(object));
            if(smithereens[0].NodeType != ExpressionType.Parameter)
                return base.Visit(node);
            var index = 0;
            while(index < smithereens.Length && smithereens[index].NodeType != ExpressionType.Call)
                ++index;
            var current = new CurrentDependencies
                {
                    Prefix = smithereens[index - 1],
                    AdditionalDependencies = new List<LambdaExpression>()
                };
            while(index < smithereens.Length && current.Prefix != null)
            {
                var shard = smithereens[index];
                switch(shard.NodeType)
                {
                case ExpressionType.Call:
                    {
                        var methodCallExpression = (MethodCallExpression)shard;
                        var method = methodCallExpression.Method;
                        var arguments = (method.IsExtension() ? methodCallExpression.Arguments.Skip(1) : methodCallExpression.Arguments).ToArray();
                        if(method.IsGenericMethod)
                            method = method.GetGenericMethodDefinition();
                        var methodProcessor = GetMethodProcessor(method);
                        if(methodProcessor == null)
                            throw new NotSupportedException("Method '" + method + "' is not supported");
                        methodProcessor(methodCallExpression.Method, current, arguments);
                        break;
                    }
                case ExpressionType.MemberAccess:
                    {
                        var member = ((MemberExpression)shard).Member;
                        if(!(current.Prefix is NewExpression))
                            current.Prefix = Expression.MakeMemberAccess(current.Prefix, member);
                        else
                        {
                            if(!current.Prefix.IsAnonymousTypeCreation() && !current.Prefix.IsTupleCreation())
                                throw new NotSupportedException("An anonymous type or a tuple creation expected but was " + ExpressionCompiler.DebugViewGetter(current.Prefix));
                            var newExpression = (NewExpression)current.Prefix;
                            var type = newExpression.Type;
                            MemberInfo[] members;
                            if(member is PropertyInfo)
                                members = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                            else
                                throw new NotSupportedException();
                            var i = Array.IndexOf(members, member);
                            if(i < 0 || i >= newExpression.Arguments.Count) throw new InvalidOperationException();
                            var newArrayExpression = (NewArrayExpression)ClearConverts(newExpression.Arguments[i]);
                            current.Prefix = newArrayExpression.Expressions.Count == 1 ? ClearConverts(newArrayExpression.Expressions[0]) : newArrayExpression;
                        }
                    }
                    break;
                case ExpressionType.ArrayIndex:
                    current.Prefix = Expression.MakeBinary(ExpressionType.ArrayIndex, current.Prefix, ((BinaryExpression)shard).Right);
                    break;
                case ExpressionType.Convert:
                    if(current.Prefix.Type == typeof(object))
                        current.Prefix = Expression.Convert(current.Prefix, shard.Type);
                    break;
                default:
                    throw new InvalidOperationException("Node type '" + shard.NodeType + "' is not supported");
                }
                ++index;
            }

            var primaryDependencies = new List<Expression>();
            if(current.Prefix != null)
            {
                if(current.Prefix is NewExpression)
                {
                    if(!current.Prefix.IsAnonymousTypeCreation() && !current.Prefix.IsTupleCreation())
                        primaryDependencies.AddRange(((NewExpression)current.Prefix).Arguments);
                    else
                    {
                        foreach(var expression in ((NewExpression)current.Prefix).Arguments.Select(ClearConverts))
                            primaryDependencies.AddRange(((NewArrayExpression)expression).Expressions.Select(ClearConverts));
                    }
                }
                else if(current.Prefix is NewArrayExpression)
                    primaryDependencies.AddRange(((NewArrayExpression)current.Prefix).Expressions.Select(ClearConverts));
                else if(IsPrimary(smithereens.Last()))
                    primaryDependencies.Add(current.Prefix);
            }

            dependencies.AddRange(current.AdditionalDependencies);

            return Expression.NewArrayInit(typeof(object), primaryDependencies.Select(dependency => Expression.Convert(dependency, typeof(object))));
        }

        private static bool IsPrimary(Expression node)
        {
            if(node.NodeType != ExpressionType.Call)
                return true;
            var method = ((MethodCallExpression)node).Method;
            if(method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();
            return method.DeclaringType != typeof(Enumerable) || (method.Name != "Any" && method.Name != "All");
        }

        private MethodProcessor GetMethodProcessor(MethodInfo method)
        {
            MethodProcessor result;
            if(methodProcessors.TryGetValue(method, out result))
                return result;
            if(method.DeclaringType == typeof(Enumerable))
            {
                switch(method.Name)
                {
                case "Sum":
                case "Max":
                case "Min":
                    return ProcessLinqSum;
                case "Single":
                case "SingleOrDefault":
                case "First":
                case "FirstOrDefault":
                    return ProcessLinqFirst;
                case "Where":
                    return ProcessLinqWhere;
                case "Any":
                case "All":
                    return ProcessLinqAny;
                case "Select":
                    return ProcessLinqSelect;
                case "SelectMany":
                    return ProcessLinqSelectMany;
                case "Aggregate":
                    return ProcessLinqAggregate;
                case "ToArray":
                case "Cast":
                    return DefaultMethodProcessor;
                default:
                    return null;
                }
            }
            if(method.IsIndexerGetter())
                return ProcessIndexer;
            return DefaultMethodProcessor;
        }

        private void ProcessIndexer(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            current.Prefix = Expression.Call(current.Prefix, method, arguments);
        }

        private void DefaultMethodProcessor(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            foreach(var argument in arguments)
                Visit(argument);
        }

        private static Expression GotoEach(Expression node)
        {
            node = node.Type.IsEnumerable() && node.NodeType != ExpressionType.NewArrayInit ? Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(node.Type.GetItemType()), node) : node;
            return new MethodReplacer(MutatorsHelperFunctions.CurrentMethod, MutatorsHelperFunctions.EachMethod).Visit(node);
        }

        private static Expression GotoCurrent(Expression node)
        {
            return node.Type.IsEnumerable() && node.NodeType != ExpressionType.NewArrayInit ? Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(node.Type.GetItemType()), node) : node;
        }

        private Expression MakeSelect(CurrentDependencies current, LambdaExpression selector)
        {
            var subExtractor = new DependenciesExtractor(selector, parameters.Concat(selector.Parameters));
            LambdaExpression[] primarySubDependencies;
            LambdaExpression[] additionalSubDependencies;
            subExtractor.Extract(out primarySubDependencies, out additionalSubDependencies);
            var prefixCurrent = MakeLambda(GotoCurrent(current.Prefix));

            current.AddSubDependencies(prefixCurrent, additionalSubDependencies);

            if(primarySubDependencies.Length > 1)
                return Expression.NewArrayInit(typeof(object), primarySubDependencies.Select(exp => Expression.Convert(prefixCurrent.Merge(exp).Body, typeof(object))));

            var primarySubDependency = primarySubDependencies.SingleOrDefault();
            if(primarySubDependency == null)
                return null;

            if(!(primarySubDependency.Body is NewExpression))
                return new AnonymousTypeEliminator().Eliminate(prefixCurrent.Merge(primarySubDependency)).Body;
            var newExpression = (NewExpression)primarySubDependency.Body;
            return Expression.New(newExpression.Constructor, newExpression.Arguments.Select(arg => prefixCurrent.Merge(Expression.Lambda(arg, primarySubDependency.Parameters)).Body));
        }

        private void ProcessLinqSum(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            var selector = (LambdaExpression)arguments.SingleOrDefault();
            if(selector != null)
                current.Prefix = MakeSelect(current, selector);
            else
            {
                current.Prefix = GotoEach(current.Prefix);
                current.AddDependency(MakeLambda(current.Prefix));
            }
            current.ReplaceCurrentWithEach();
        }

        private void ProcessLinqAggregate(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            switch(arguments.Length)
            {
            case 2:
                {
                    var seed = arguments[0];
                    var selector = (LambdaExpression)arguments[1];
                    current.AddDependency(MakeLambda(seed));
                    current.Prefix = MakeSelect(current, Expression.Lambda(selector.Body, selector.Parameters[1]));
                    current.ReplaceCurrentWithEach();
                    break;
                }
            default:
                throw new NotSupportedException(string.Format("Method '{0}' is not supported", method));
            }
        }

        private void ProcessLinqFirst(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            if(current.Prefix.Type.IsArray && method.ReturnType == current.Prefix.Type.GetItemType())
                current.Prefix = GotoEach(current.Prefix);
            var predicate = (LambdaExpression)arguments.SingleOrDefault();
            if(predicate == null)
                current.AddDependency(MakeLambda(current.Prefix));
            else
            {
                var subDependencies = predicate.ExtractDependencies(parameters.Concat(predicate.Parameters));
                current.AddSubDependencies(MakeLambda(current.Prefix), subDependencies);
            }
            current.ReplaceCurrentWithEach();
        }

        private void ProcessLinqWhere(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            if(current.Prefix.Type == typeof(string))
                return;
            var prefixCurrent = MakeLambda(GotoCurrent(current.Prefix));
            var predicate = (LambdaExpression)arguments.SingleOrDefault();
            if(predicate == null)
                current.AddDependency(prefixCurrent);
            else
            {
                var subDependencies = predicate.ExtractDependencies(parameters.Concat(predicate.Parameters));
                current.AddSubDependencies(prefixCurrent, subDependencies);
            }
        }

        private void ProcessLinqAny(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            if(current.Prefix.Type == typeof(string))
                return;
            var prefixEach = MakeLambda(GotoEach(current.Prefix));
            var predicate = (LambdaExpression)arguments.SingleOrDefault();
            if(predicate == null)
                current.AddDependency(prefixEach);
            else
            {
                var subDependencies = predicate.ExtractDependencies(parameters.Concat(predicate.Parameters));
                current.AddSubDependencies(prefixEach, subDependencies);
            }
            current.ReplaceCurrentWithEach();
        }

        private void ProcessLinqSelect(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            current.Prefix = MakeSelect(current, (LambdaExpression)arguments.Single());
        }

        private void ProcessLinqSelectMany(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            var collectionSelector = (LambdaExpression)arguments[0];
            var collection = MakeSelect(current, collectionSelector);
            if(collection == null)
            {
                current.Prefix = null;
                return;
            }
            if(arguments.Length == 1)
            {
                current.Prefix = collection;
                return;
            }
            var resultSelector = (LambdaExpression)arguments[1];
            var subExtractor = new DependenciesExtractor(resultSelector, parameters.Concat(resultSelector.Parameters));
            LambdaExpression[] primarySubDependencies;
            LambdaExpression[] additionalSubDependencies;
            subExtractor.Extract(out primarySubDependencies, out additionalSubDependencies);
            var prefixCurrent = MakeLambda(GotoCurrent(current.Prefix));
            var collectionCurrent = MakeLambda(GotoCurrent(collection));
            current.AddSubDependencies(prefixCurrent, collectionCurrent, additionalSubDependencies);
            var primarySubDependency = primarySubDependencies.Single();
            if(!(primarySubDependency.Body is NewExpression))
                current.Prefix = new AnonymousTypeEliminator().Eliminate(new ExpressionMerger(prefixCurrent, collectionCurrent).Merge(primarySubDependency)).Body;
            else
            {
                var newExpression = (NewExpression)primarySubDependency.Body;
                current.Prefix = Expression.New(newExpression.Constructor, newExpression.Arguments.Select(arg => new ExpressionMerger(prefixCurrent, collectionCurrent).Merge(Expression.Lambda(arg, primarySubDependency.Parameters)).Body));
            }
        }

        private void ProcessExternalCurrent(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            var prefix = current.Prefix;
            if(prefix.IsAnonymousTypeCreation() || prefix.IsTupleCreation() || IsExternalCurrentCall(prefix))
                return;
            if(IsEachOrCurrentCall(prefix))
                prefix = ((MethodCallExpression)prefix).Arguments[0];
            current.Prefix = Expression.Call(DependenciesExtractorHelper.ExternalCurrentMethod.MakeGenericMethod(prefix.Type.GetItemType()), prefix);
        }

        private static bool IsExternalCurrentCall(Expression node)
        {
            var methodCallExpression = node as MethodCallExpression;
            if(methodCallExpression == null)
                return false;
            return methodCallExpression.Method.IsGenericMethod && methodCallExpression.Method.GetGenericMethodDefinition() == DependenciesExtractorHelper.ExternalCurrentMethod;
        }

        private static bool IsEachOrCurrentCall(Expression node)
        {
            var methodCallExpression = node as MethodCallExpression;
            if(methodCallExpression == null)
                return false;
            return methodCallExpression.Method.IsEachMethod() || methodCallExpression.Method.IsCurrentMethod();
        }

        private void ProcessCurrentIndex(MethodInfo method, CurrentDependencies current, Expression[] arguments)
        {
            current.Prefix = Expression.ArrayLength(((MethodCallExpression)current.Prefix).Arguments.Single());
        }

        private readonly HashSet<ParameterExpression> parameters;

        private readonly Dictionary<MethodInfo, MethodProcessor> methodProcessors;

        private readonly List<LambdaExpression> dependencies = new List<LambdaExpression>();
        private readonly LambdaExpression lambda;

        private class CurrentDependencies
        {
            public void ReplaceCurrentWithEach()
            {
                var replacer = new MethodReplacer(MutatorsHelperFunctions.CurrentMethod, MutatorsHelperFunctions.EachMethod);
                for(var i = 0; i < AdditionalDependencies.Count; ++i)
                    AdditionalDependencies[i] = (LambdaExpression)replacer.Visit(AdditionalDependencies[i]);
                Prefix = replacer.Visit(Prefix);
            }

            public void AddSubDependencies(LambdaExpression prefix, LambdaExpression[] subDependencies)
            {
                foreach(var subDependency in subDependencies)
                {
                    var refinedSubDependency = new AnonymousTypeEliminator().Eliminate(prefix.Merge(subDependency));
                    AddDependency(refinedSubDependency);
                }
            }

            public void AddSubDependencies(LambdaExpression prefix1, LambdaExpression prefix2, LambdaExpression[] subDependencies)
            {
                foreach(var subDependency in subDependencies)
                {
                    var refinedSubDependency = new AnonymousTypeEliminator().Eliminate(new ExpressionMerger(prefix1, prefix2).Merge(subDependency));
                    AddDependency(refinedSubDependency);
                }
            }

            public void AddDependency(LambdaExpression dependency)
            {
                if(dependency.Body.NodeType == ExpressionType.NewArrayInit)
                    AdditionalDependencies.AddRange(((NewArrayExpression)dependency.Body).Expressions.Select(expression => Expression.Lambda(ClearConverts(expression), dependency.Parameters)));
                else
                    AdditionalDependencies.Add(dependency);
            }

            public Expression Prefix { get; set; }
            public List<LambdaExpression> AdditionalDependencies { get; set; }
        }

        private class AnonymousTypeEliminator : ExpressionVisitor
        {
            public LambdaExpression Eliminate(LambdaExpression lambda)
            {
                var body = Visit(lambda.Body);
                return Expression.Lambda(body, lambda.Parameters);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if(!node.Expression.IsAnonymousTypeCreation() && !node.Expression.IsTupleCreation())
                    return base.VisitMember(node);
                var newExpression = (NewExpression)Visit(node.Expression);
                var member = node.Member;
                var type = newExpression.Type;
                MemberInfo[] members;
                if(member is FieldInfo)
                    members = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                else if(member is PropertyInfo)
                    members = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                else throw new NotSupportedException();
                var i = Array.IndexOf(members, member);
                if(i < 0 || i >= newExpression.Arguments.Count) throw new InvalidOperationException();
                var newArrayExpression = (NewArrayExpression)ClearConverts(newExpression.Arguments[i]);
                return newArrayExpression.Expressions.Count == 1 ? ClearConverts(newArrayExpression.Expressions[0]) : newArrayExpression;
            }
        }
    }
}