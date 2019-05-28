using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;
using GrobExp.Mutators.ModelConfiguration;

namespace Mutators.Tests.ConfigurationTests
{
    public static class Edge
    {
        public static ModelConfigurationEdge Create<TSource, TDest>(Expression<Func<TSource, TDest>> edge)
        {
            switch (edge.Body)
            {
            case MemberExpression memberExpression:
                return new ModelConfigurationEdge(memberExpression.Member);

            case UnaryExpression unaryExpression:
                switch (unaryExpression.NodeType)
                {
                case ExpressionType.Convert:
                    return new ModelConfigurationEdge(unaryExpression.Type);
                case ExpressionType.ArrayLength:
                    return new ModelConfigurationEdge(ModelConfigurationEdge.ArrayLengthProperty);
                }
                throw new InvalidOperationException();

            case BinaryExpression binaryExpression:
                switch (binaryExpression.NodeType)
                {
                case ExpressionType.ArrayIndex:
                    int value;
                    if (binaryExpression.Right is ConstantExpression constantExpression)
                        value = (int)constantExpression.Value;
                    else
                        value = Expression.Lambda<Func<int>>(Expression.Convert(binaryExpression.Right, typeof(int))).Compile()();
                    return new ModelConfigurationEdge(value);
                }
                throw new InvalidOperationException();

            case MethodCallExpression methodCallExpression:
                if (methodCallExpression.Method.IsIndexerGetter())
                    return new ModelConfigurationEdge(methodCallExpression.Arguments.Select(exp => ((ConstantExpression)exp).Value).ToArray());
                return new ModelConfigurationEdge(methodCallExpression.Method);

            default:
                throw new InvalidOperationException();
            }
        }

        public static ModelConfigurationEdge Each = ModelConfigurationEdge.Each;
    }
}