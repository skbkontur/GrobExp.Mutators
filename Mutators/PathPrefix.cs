using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    internal class PathPrefix
    {
        public PathPrefix(Expression path, ParameterExpression parameter, ParameterExpression index = null)
        {
            Path = path;
            Parameter = parameter;
            Index = index;
        }

        public Expression Path { get; }
        public ParameterExpression Parameter { get; }
        public ParameterExpression Index { get; }
    }
}