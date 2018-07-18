using System.Linq.Expressions;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public class AssignLogInfo
    {
        public AssignLogInfo(Expression path, Expression value)
        {
            Path = path;
            Value = value;
        }

        public Expression Path { get; private set; }
        public Expression Value { get; private set; }
    }
}