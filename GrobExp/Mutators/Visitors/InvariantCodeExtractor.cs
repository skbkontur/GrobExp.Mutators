using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class InvariantCodeExtractor : ExpressionVisitor
    {
        public InvariantCodeExtractor(IEnumerable<ParameterExpression> invariantParameters)
        {
            this.invariantParameters = new HashSet<ParameterExpression>(invariantParameters);
            extractableExpressions = new List<Expression>();
            stack = new Stack<NodeInfo>();
        }

        public Expression[] Extract(Expression expression)
        {
            extractableExpressions.Clear();
            stack.Clear();
            stack.Push(new NodeInfo(null));
            Visit(expression);
            return new ExternalExpressionsTaker(extractableExpressions).Take(expression).ToArray();
        }

        public override Expression Visit(Expression node)
        {
            stack.Push(new NodeInfo(node));
            var visitedNode = base.Visit(node);
            var child = stack.Peek();
            stack.Pop();
            var parent = stack.Peek();
            foreach (var dependency in child.Dependencies)
            {
                parent.Dependencies.Add(dependency);
            }
            return visitedNode;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            stack.Peek().Dependencies.Add(node);
            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            var visitedNode = base.VisitBlock(node);
            FilterDependencies(node);
            if (ExtractableBlockWithLoop(node))
                extractableExpressions.Add(node);
            return visitedNode;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var visitedNode = base.VisitLambda(node);
            FilterDependencies(node);
            return visitedNode;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if(node.Method.IsDynamicMethod())
                return node;
            var visitedNode = base.VisitMethodCall(node);
            if (ExtractableMethodCall(node))
                extractableExpressions.Add(node);
            return visitedNode;
        }

        private bool ExtractableMethodCall(MethodCallExpression node)
        {
            var currentNode = stack.Peek();
            return node.Type != typeof(void) && currentNode.Dependencies.All(d => invariantParameters.Contains(d));
        }

        private void FilterDependencies(LambdaExpression node)
        {
            var nodeInfo = stack.Peek();
            foreach (var variable in node.Parameters)
            {
                nodeInfo.Dependencies.Remove(variable);
            }
        }

        private void FilterDependencies(BlockExpression node)
        {
            var nodeInfo = stack.Peek();
            foreach (var variable in node.Variables)
            {
                nodeInfo.Dependencies.Remove(variable);
            }
        }

        private bool ExtractableBlockWithLoop(BlockExpression node)
        {
            if (node.Type == typeof(void) || node.Expressions.Count(e => e.NodeType == ExpressionType.Loop) != 1)
                return false;
            return stack.Peek().Dependencies.All(d => invariantParameters.Contains(d));
        }

        private readonly List<Expression> extractableExpressions;
        private readonly HashSet<ParameterExpression> invariantParameters;
        private readonly Stack<NodeInfo> stack;

        private class NodeInfo
        {
            public NodeInfo(Expression expression)
            {
                Expression = expression;
                Dependencies = new HashSet<ParameterExpression>();
            }

            public Expression Expression { get; private set; }

            public HashSet<ParameterExpression> Dependencies { get; private set; }
        }
    }
}
