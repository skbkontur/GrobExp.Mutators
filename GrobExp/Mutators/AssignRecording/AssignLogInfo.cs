using System.Linq.Expressions;

namespace GrobExp.Mutators.AssignRecording
{
    public class AssignLogInfo
    {
        public Expression path;
        public Expression value;

        public AssignLogInfo(Expression path, Expression value)
        {
            this.path = path;
            this.value = value;
        }
    }
}