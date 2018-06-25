using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    public class PathPrefix
    {
        public PathPrefix(Expression path, ParameterExpression parameter, ParameterExpression index = null)
        {
            Path = path;
            Parameter = parameter;
            Index = index;
        }

        public Expression Path { get; private set; }
        public ParameterExpression Parameter { get; private set; }
        public ParameterExpression Index { get; private set; }
    }
}