using System.Collections.Generic;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public interface IMutatorsValidationRecorder
    {
        List<RecordNode> GetErrorRecords();
        void Stop();
    }
}