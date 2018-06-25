namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public class AssignRecorderInitializer
    {
        public static IMutatorsAssignRecorder StartAssignRecorder()
        {
            return MutatorsAssignRecorder.StartRecording();
        }
    }
}