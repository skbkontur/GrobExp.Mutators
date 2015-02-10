using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class ExternalExpressionsExtractor : ExpressionVisitor
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
            if(node == null || node.NodeType == ExpressionType.Constant || node.NodeType == ExpressionType.Parameter || node.NodeType == ExpressionType.Default || IsDynamicMethodCall(node as MethodCallExpression))
                return node;
            var parameters = parametersExtractor.Extract(node);
            if(node.Type == typeof(void) || parameters.Any(parameter => internalVariables.Contains(parameter)) || objectCreationSearcher.ContainsObjectCreation(node))
                return base.Visit(node);
            externalExpressions.Add(node);
            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach(var variable in node.Variables)
                internalVariables.Add(variable);
            var result = base.VisitBlock(node);
            foreach(var variable in node.Variables)
                internalVariables.Remove(variable);
            return result;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            foreach(var parameter in node.Parameters)
                internalVariables.Add(parameter);
            var result = base.VisitLambda(node);
            foreach(var parameter in node.Parameters)
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

    public class ObjectCreationSearcher : ExpressionVisitor
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

        private bool result;
    }
}