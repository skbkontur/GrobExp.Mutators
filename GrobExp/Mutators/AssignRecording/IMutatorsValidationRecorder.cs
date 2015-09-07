using System.Collections.Generic;

namespace GrobExp.Mutators.AssignRecording
{
    public interface IMutatorsValidationRecorder
    {
        List<RecordNode> GetRecords();
        void Stop();
    }
}