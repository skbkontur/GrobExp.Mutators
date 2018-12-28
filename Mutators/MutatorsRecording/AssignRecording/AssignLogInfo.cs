using System.Linq.Expressions;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    internal class AssignLogInfo
    {
        public AssignLogInfo(Expression path, Expression value)
        {
            Path = path;
            Value = value;
        }

        public Expression Path { get; }
        public Expression Value { get; }
    }
}