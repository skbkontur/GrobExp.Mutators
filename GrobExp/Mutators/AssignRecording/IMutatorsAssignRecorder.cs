using System.Collections.Generic;

namespace GrobExp.Mutators.AssignRecording
{
    public interface IMutatorsAssignRecorder
    {
        List<RecordNode> GetRecords();
        void Stop();
    }
}