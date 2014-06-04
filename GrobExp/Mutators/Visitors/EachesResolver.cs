using System;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.Visitors
{
    public class EachesResolver : ExpressionVisitor
    {
        public EachesResolver(Expression[] indexes)
        {
            this.indexes = indexes;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if(node.Method.IsCurrentIndexMethod())
            {
                var path = node.Arguments.Single();
                var currents = eachesCounter.CountEaches(path) - 1;
                var index = GetPiece(currents);
                return index;
            }
            if(node.Method.IsCurrentMethod() || node.Method.IsEachMethod())
            {
                var path = node.Arguments.Single();
                var currents = eachesCounter.CountEaches(path);
                var index = GetPiece(currents);
                var array = Visit(path);
                if(!array.Type.IsArray)
                {
//                    // Filtered array
//                    if(array.NodeType != ExpressionType.Call)
//                        throw new NotSupportedException("Expected an array member access or a filtered with Enumerable.Where array member access");
//                    var methodCallExpression = (MethodCallExpression)array;
////                    if(!methodCallExpression.Method.IsWhereMethod())
////                        throw new NotSupportedException("Expected an array member access or a filtered with Enumerable.Where array member access");
//                    array = methodCallExpression.Arguments[0];
                    array = Expression.Call(typeof(Enumerable), "ToArray", new[] {array.Type.GetItemType()}, array);
                }
                return Expression.ArrayIndex(array, index);
            }
            return base.VisitMethodCall(node);
        }

        private Expression GetPiece(int currents)
        {
            if(currents >= indexes.Length)
                throw new InvalidOperationException("Too many currents");
            return indexes[currents];
        }

        private readonly Expression[] indexes;

        private readonly EachesCounter eachesCounter = new EachesCounter();

        private class EachesCounter : ExpressionVisitor
        {
            public int CountEaches(Expression node)
            {
                eaches = 0;
                Visit(node);
                return eaches;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if(node.Method.IsEachMethod() || node.Method.IsCurrentMethod())
                    ++eaches;
                return base.VisitMethodCall(node);
            }

            private int eaches;
        }
    }
}