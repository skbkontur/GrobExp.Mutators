using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    public interface IPathFormatter
    {
        Expression GetFormattedPath(Expression[] paths);
    }
}