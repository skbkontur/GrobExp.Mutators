namespace GrobExp.Mutators.AssignRecording
{
    public class AssignRecorderInitializer
    {
        public static IMutatorsAssignRecorder StartAssignRecorder()
        {
            return MutatorsAssignRecorder.StartRecording();
        }
    }
}