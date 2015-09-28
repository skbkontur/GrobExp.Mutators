using System.Collections.Generic;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public interface IMutatorsAssignRecorder
    {
        List<RecordNode> GetRecords();
        void Stop();
    }
}