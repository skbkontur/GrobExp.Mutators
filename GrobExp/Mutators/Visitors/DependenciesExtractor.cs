using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.Visitors
{
    public class DependenciesExtractor : ExpressionVisitor
    {
        public DependenciesExtractor(LambdaExpression lambda, IEnumerable<ParameterExpression> parameters)
        {
            this.lambda = lambda;
            this.parameters = new HashSet<ParameterExpression>(parameters);
            methodProcessors = new Dictionary<MethodInfo, MethodProcessor>
                {
                    {MutatorsHelperFunctions.CurrentMethod, ProcessCurrent},
                    {MutatorsHelperFunctions.EachMethod, ProcessCurrent},
                    {MutatorsHelperFunctions.CurrentIndexMethod, ProcessCurrentIndex},
                };
        }

        public override Expression Visit(Expression node)
        {
            return node.IsLinkOfChain(true, true) ? VisitChain(node) : base.Visit(node);
        }

        public LambdaExpression[] Extract(out LambdaExpression[] primaryDependencies, out LambdaExpression[] additionalDependencies)
        {
            var body = Visit(lambda.Body);
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
                primaryDependencies = primaryDependenciez.GroupBy(exp => ExpressionCompiler.DebugViewGetter(exp)).Select(grouping => grouping.First()).ToArray();
            }
            additionalDependencies = dependencies.Select(dependency => Expression.Lambda(ClearConverts(dependency.Body), dependency.Parameters)).Where(dependency =>
                {
                    var root = dependency.Body.SmashToSmithereens()[0];
                    return root.NodeType == ExpressionType.Parameter && parameters.Contains((ParameterExpression)root);
                }).GroupBy(exp => ExpressionCompiler.DebugViewGetter(exp)).Select(grouping => grouping.First()).ToArray();
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
            throw new NotSupportedException();
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var test = Visit(node.Test);
            var ifTrue = Visit(node.IfTrue);
            var ifFalse = Visit(node.IfFalse);
            if(test != null)
            {
                foreach(var dependency in ((NewArrayExpression)test).Expressions)
                    dependencies.Add(Expression.Lambda(dependency, parameters));
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
            foreach (var argument in node.Arguments)
            {
                var arg = Visit(argument);
                if (arg != null)
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

        private delegate Expression MethodProcessor(MethodInfo method, Expression prefix, Expression[] arguments);

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
            var prefix = smithereens[index - 1];
            while(index < smithereens.Length)
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
                        prefix = methodProcessor(methodCallExpression.Method, prefix, arguments);
                        break;
                    }
                case ExpressionType.MemberAccess:
                    {
                        var member = ((MemberExpression)shard).Member;
                        if(!(prefix is NewExpression))
                            prefix = Expression.MakeMemberAccess(prefix, member);
                        else
                        {
                            if(!prefix.IsAnonymousTypeCreation() && !prefix.IsTupleCreation())
                                throw new NotSupportedException("An anonymous type or a tuple creation expected but was " + ExpressionCompiler.DebugViewGetter(prefix));
                            var newExpression = (NewExpression)prefix;
                            var type = newExpression.Type;
                            MemberInfo[] members;
                            if(member is PropertyInfo)
                                members = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                            else
                                throw new NotSupportedException();
                            var i = Array.IndexOf(members, member);
                            if(i < 0 || i >= newExpression.Arguments.Count) throw new InvalidOperationException();
                            var newArrayExpression = (NewArrayExpression)ClearConverts(newExpression.Arguments[i]);
                            prefix = newArrayExpression.Expressions.Count == 1 ? ClearConverts(newArrayExpression.Expressions[0]) : newArrayExpression;
                        }
                    }
                    break;
                case ExpressionType.ArrayIndex:
                    prefix = Expression.MakeBinary(ExpressionType.ArrayIndex, prefix, ((BinaryExpression)shard).Right);
                    break;
                }
                ++index;
            }

            var primaryDependencies = new List<Expression>();
            if(prefix is NewExpression)
            {
                if(!prefix.IsAnonymousTypeCreation() && !prefix.IsTupleCreation())
                    primaryDependencies.AddRange(((NewExpression)prefix).Arguments);
                else
                {
                    foreach(var expression in ((NewExpression)prefix).Arguments.Select(ClearConverts))
                        primaryDependencies.AddRange(((NewArrayExpression)expression).Expressions.Select(ClearConverts));
                }
            }
            else if(prefix is NewArrayExpression)
                primaryDependencies.AddRange(((NewArrayExpression)prefix).Expressions.Select(ClearConverts));
            else if(IsPrimary(smithereens.Last()))
                primaryDependencies.Add(prefix);

            return Expression.NewArrayInit(typeof(object), primaryDependencies.Select(dependency => Expression.Convert(dependency, typeof(object))));
        }

        private static bool IsPrimary(Expression node)
        {
            if(node.NodeType != ExpressionType.Call)
                return true;
            var method = ((MethodCallExpression)node).Method;
            if(method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();
            return method.DeclaringType != typeof(Enumerable) || method.Name != "Any";
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
                    return ProcessLinqAggregate;
                case "Single":
                case "SingleOrDefault":
                case "First":
                case "FirstOrDefault":
                    return ProcessLinqFirst;
                case "Where":
                case "Any":
                    return ProcessLinqWhere;
                case "Select":
                    return ProcessLinqSelect;
                case "SelectMany":
                    return ProcessLinqSelectMany;
                case "ToArray":
                    return DefaultMethodProcessor;
                default:
                    return null;
                }
            }
            if(method.IsIndexerGetter())
                return ProcessIndexer;
            return DefaultMethodProcessor;
        }

        private Expression ProcessIndexer(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            return Expression.Call(prefix, method, arguments);
        }

        private Expression DefaultMethodProcessor(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            foreach(var argument in arguments)
                Visit(argument);
            return prefix;
        }

        private static Expression GotoEach(Expression node)
        {
            return node.Type.IsArray && node.NodeType != ExpressionType.NewArrayInit ? Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(node.Type.GetElementType()), node) : node;
        }

        private void AddSubDependencies(LambdaExpression prefix, LambdaExpression[] subDependencies)
        {
            foreach(var subDependency in subDependencies)
            {
                var refinedSubDependency = new AnonymousTypeEliminator().Eliminate(prefix.Merge(subDependency));
                AddDependency(refinedSubDependency);
            }
        }

        private void AddSubDependencies(LambdaExpression prefix1, LambdaExpression prefix2, LambdaExpression[] subDependencies)
        {
            foreach(var subDependency in subDependencies)
            {
                var refinedSubDependency = new AnonymousTypeEliminator().Eliminate(new ExpressionMerger(prefix1, prefix2).Merge(subDependency));
                AddDependency(refinedSubDependency);
            }
        }

        private void AddDependency(LambdaExpression dependency)
        {
            if(dependency.Body.NodeType == ExpressionType.NewArrayInit)
                dependencies.AddRange(((NewArrayExpression)dependency.Body).Expressions.Select(expression => Expression.Lambda(ClearConverts(expression), dependency.Parameters)));
            else
                dependencies.Add(dependency);
        }

        private Expression MakeSelect(Expression prefix, LambdaExpression selector)
        {
            var subExtractor = new DependenciesExtractor(selector, parameters.Concat(selector.Parameters));
            LambdaExpression[] primarySubDependencies;
            LambdaExpression[] additionalSubDependencies;
            var subDependencies = subExtractor.Extract(out primarySubDependencies, out additionalSubDependencies);
            var prefixEach = MakeLambda(GotoEach(prefix));
            AddSubDependencies(prefixEach, additionalSubDependencies);
            var primarySubDependency = primarySubDependencies.Single();
            if(!(primarySubDependency.Body is NewExpression))
                return new AnonymousTypeEliminator().Eliminate(prefixEach.Merge(primarySubDependency)).Body;
            var newExpression = (NewExpression)primarySubDependency.Body;
            return Expression.New(newExpression.Constructor, newExpression.Arguments.Select(arg => prefixEach.Merge(Expression.Lambda(arg, primarySubDependency.Parameters)).Body));
        }

        private Expression ProcessLinqAggregate(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            var selector = (LambdaExpression)arguments.SingleOrDefault();
            if(selector == null)
            {
                prefix = GotoEach(prefix);
                AddDependency(MakeLambda(prefix));
                return prefix;
            }
            return MakeSelect(prefix, selector);
        }

        private Expression ProcessLinqFirst(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            if(prefix.Type.IsArray && method.ReturnType == prefix.Type.GetItemType())
                prefix = GotoEach(prefix);
            var predicate = (LambdaExpression)arguments.SingleOrDefault();
            if(predicate == null)
            {
                AddDependency(MakeLambda(prefix));
                return prefix;
            }
            var subDependencies = predicate.ExtractDependencies(parameters.Concat(predicate.Parameters));
            AddSubDependencies(MakeLambda(prefix), subDependencies);
            return prefix;
        }

        private Expression ProcessLinqWhere(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            if(prefix.Type == typeof(string))
                return prefix;
            var predicate = (LambdaExpression)arguments.Single();
            var subDependencies = predicate.ExtractDependencies(parameters.Concat(predicate.Parameters));
            AddSubDependencies(MakeLambda(GotoEach(prefix)), subDependencies);
            return prefix;
        }

        private Expression ProcessLinqSelect(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            return MakeSelect(prefix, (LambdaExpression)arguments.Single());
        }

        private Expression ProcessLinqSelectMany(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            var collectionSelector = (LambdaExpression)arguments[0];
            var collection = MakeSelect(prefix, collectionSelector);
            if(arguments.Length == 1)
                return collection;
            var resultSelector = (LambdaExpression)arguments[1];
            var subExtractor = new DependenciesExtractor(resultSelector, parameters.Concat(resultSelector.Parameters));
            LambdaExpression[] primarySubDependencies;
            LambdaExpression[] additionalSubDependencies;
            var subDependencies = subExtractor.Extract(out primarySubDependencies, out additionalSubDependencies);
            var prefixEach = MakeLambda(GotoEach(prefix));
            var collectionEach = MakeLambda(GotoEach(collection));
            AddSubDependencies(prefixEach, collectionEach, additionalSubDependencies);
            var primarySubDependency = primarySubDependencies.Single();
            if(!(primarySubDependency.Body is NewExpression))
                return new AnonymousTypeEliminator().Eliminate(new ExpressionMerger(prefixEach, collectionEach).Merge(primarySubDependency)).Body;
            var newExpression = (NewExpression)primarySubDependency.Body;
            return Expression.New(newExpression.Constructor, newExpression.Arguments.Select(arg => new ExpressionMerger(prefixEach, collectionEach).Merge(Expression.Lambda(arg, primarySubDependency.Parameters)).Body));
        }

        private Expression ProcessCurrent(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            if(prefix.IsAnonymousTypeCreation() || prefix.IsTupleCreation())
                return prefix;
            return Expression.Call( /*method.MakeGenericMethod(prefix.Type.GetElementType())*/method, prefix);
        }

        private Expression ProcessCurrentIndex(MethodInfo method, Expression prefix, Expression[] arguments)
        {
            var methodCallExpression = ((MethodCallExpression)prefix);
            if(!methodCallExpression.Method.IsEachMethod() && !methodCallExpression.Method.IsCurrentMethod())
                throw new NotSupportedException();
            return Expression.ArrayLength(methodCallExpression.Arguments.Single());
        }

        private readonly HashSet<ParameterExpression> parameters;

        private readonly Dictionary<MethodInfo, MethodProcessor> methodProcessors;

        private readonly List<LambdaExpression> dependencies = new List<LambdaExpression>();
        private readonly LambdaExpression lambda;

        private class AnonymousTypeEliminator : ExpressionVisitor
        {
            public LambdaExpression Eliminate(LambdaExpression lambda)
            {
                var body = Visit(lambda.Body);
                return Expression.Lambda(body, lambda.Parameters);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if(!node.Expression.IsAnonymousTypeCreation())
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