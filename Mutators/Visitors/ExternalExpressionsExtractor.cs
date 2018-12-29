using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Compiler;

namespace GrobExp.Mutators.Visitors
{
    internal class HackedExternalExpressionsExtractor : ExpressionVisitor
    {
        public HackedExternalExpressionsExtractor(IEnumerable<ParameterExpression> internalVariables)
        {
            this.givenVariables = new HashSet<ParameterExpression>(internalVariables);
        }

        public Expression[] Extract(Expression expression)
        {
            nodesStack.Push(new NodeInfo(-1));
            Visit(expression);
            var externalExpressions = new ExternalExpressionsTaker(externalNodes).Take(expression);
            var result = externalExpressions.GroupBy(exp => ExpressionCompiler.DebugViewGetter(exp))
                                            .Select(grouping => grouping.First()).ToArray();
            externalExpressions.Clear();
            return result;
        }

        public override Expression Visit(Expression node)
        {
            var parentInfo = nodesStack.Peek();
            int height = parentInfo.Height + 1;

            // todo обработать константы сложных типов правильно
            if (node == null || node.NodeType == ExpressionType.Constant || node.NodeType == ExpressionType.Default)
                return node;

            if (node.NodeType == ExpressionType.Call && ((MethodCallExpression)node).Method.IsDynamicMethod())
            {
                parentInfo.HasObjectCreation = true;
                return node;
            }

            if (node.NodeType == ExpressionType.Parameter)
            {
                var param = (ParameterExpression)node;
                int declarationHeight;
                if (internalVariables.TryGetValue(param, out declarationHeight))
                    parentInfo.MinimalDeclarationHeight = Math.Min(parentInfo.MinimalDeclarationHeight, declarationHeight);
                else if (givenVariables.Contains(param))
                    parentInfo.MinimalDeclarationHeight = -1;
                return node;
            }

            if (node.NodeType == ExpressionType.Block || node.NodeType == ExpressionType.Lambda)
            {
                IEnumerable<ParameterExpression> variables = node.NodeType == ExpressionType.Block
                                                                 ? ((BlockExpression)node).Variables
                                                                 : ((LambdaExpression)node).Parameters;
                foreach (var variable in variables)
                    internalVariables.Add(variable, height);
            }

            var myInfo = new NodeInfo(height);
            nodesStack.Push(myInfo);
            base.Visit(node);
            nodesStack.Pop();

            if (node.NodeType == ExpressionType.Block || node.NodeType == ExpressionType.Lambda)
            {
                IEnumerable<ParameterExpression> variables = node.NodeType == ExpressionType.Block
                                                                 ? ((BlockExpression)node).Variables
                                                                 : ((LambdaExpression)node).Parameters;
                foreach (var variable in variables)
                    internalVariables.Remove(variable);
            }

            if (node.NodeType == ExpressionType.Invoke || node.NodeType == ExpressionType.New ||
                node.NodeType == ExpressionType.NewArrayBounds || node.NodeType == ExpressionType.NewArrayInit || node.NodeType == ExpressionType.Label || node.NodeType == ExpressionType.Goto)
                myInfo.HasObjectCreation = true;

            parentInfo.MinimalDeclarationHeight = Math.Min(parentInfo.MinimalDeclarationHeight, myInfo.MinimalDeclarationHeight);
            parentInfo.HasObjectCreation |= myInfo.HasObjectCreation;

            if (node.Type == typeof(void) || myInfo.MinimalDeclarationHeight < height || myInfo.HasObjectCreation)
                return node;

            externalNodes.Add(node);
            return node;
        }

        private readonly HashSet<ParameterExpression> givenVariables;
        private readonly Dictionary<ParameterExpression, int> internalVariables = new Dictionary<ParameterExpression, int>();
        private readonly Stack<NodeInfo> nodesStack = new Stack<NodeInfo>();
        private readonly HashSet<Expression> externalNodes = new HashSet<Expression>();

        private class NodeInfo
        {
            public NodeInfo(int height)
            {
                Height = height;
                MinimalDeclarationHeight = int.MaxValue;
                HasObjectCreation = false;
            }

            public int Height { get; private set; }
            public int MinimalDeclarationHeight { get; set; }
            public bool HasObjectCreation { get; set; }
        }
    }

    internal class ExternalExpressionsExtractor : ExpressionVisitor
    {
        public ExternalExpressionsExtractor(IEnumerable<ParameterExpression> internalVariables)
        {
            this.internalVariables = new HashSet<ParameterExpression>(internalVariables);
        }

        public Expression[] Extract(Expression expression)
        {
            Visit(expression);
            var result = externalExpressions.GroupBy(exp => ExpressionCompiler.DebugViewGetter(exp)).Select(grouping => grouping.First()).ToArray();
            externalExpressions.Clear();
            return result;
        }

        public override Expression Visit(Expression node)
        {
            // todo обработать константы сложных типов правильно
            if (node == null || node.NodeType == ExpressionType.Constant || node.NodeType == ExpressionType.Parameter || node.NodeType == ExpressionType.Default || IsDynamicMethodCall(node as MethodCallExpression))
                return node;
            var parameters = parametersExtractor.Extract(node);
            if (node.Type == typeof(void) || parameters.Any(parameter => internalVariables.Contains(parameter)) || objectCreationSearcher.ContainsObjectCreation(node))
                return base.Visit(node);
            externalExpressions.Add(node);
            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach (var variable in node.Variables)
                internalVariables.Add(variable);
            var result = base.VisitBlock(node);
            foreach (var variable in node.Variables)
                internalVariables.Remove(variable);
            return result;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            foreach (var parameter in node.Parameters)
                internalVariables.Add(parameter);
            var result = base.VisitLambda(node);
            foreach (var parameter in node.Parameters)
                internalVariables.Remove(parameter);
            return result;
        }

        private static bool IsDynamicMethodCall(MethodCallExpression node)
        {
            return node != null && node.Method.IsDynamicMethod();
        }

        private readonly HashSet<ParameterExpression> internalVariables;
        private readonly ParametersExtractor parametersExtractor = new ParametersExtractor();
        private readonly ObjectCreationSearcher objectCreationSearcher = new ObjectCreationSearcher();
        private readonly List<Expression> externalExpressions = new List<Expression>();
    }

    internal class ObjectCreationSearcher : ExpressionVisitor
    {
        public bool ContainsObjectCreation(Expression expression)
        {
            result = false;
            Visit(expression);
            return result;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            result = true;
            return base.VisitInvocation(node);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            result = true;
            return base.VisitNewArray(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            result = true;
            return base.VisitNew(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsDynamicMethod())
                result = true;
            return base.VisitMethodCall(node);
        }

        private bool result;
    }
}