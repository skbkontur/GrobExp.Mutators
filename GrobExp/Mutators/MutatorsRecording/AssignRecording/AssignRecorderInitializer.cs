using System;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public class AssignRecorderInitializer
    {
        public static IMutatorsAssignRecorder StartAssignRecorder(Type[] excludedFromCoverage = null)
        {
            return MutatorsAssignRecorder.StartRecording(excludedFromCoverage);
        }
    }
}