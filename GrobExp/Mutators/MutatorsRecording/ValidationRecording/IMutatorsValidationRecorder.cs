using System.Collections.Generic;

using GrobExp.Mutators.MutatorsRecording.AssignRecording;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public interface IMutatorsValidationRecorder
    {
        List<RecordNode> GetRecords();
        void Stop();
    }
}