namespace GrobExp.Mutators
{
    public class AssignRecorderInitializer
    {
        public static IMutatorsAssignRecorder StartAssignRecorder()
        {
            return MutatorsAssignRecorder.StartRecording();
        }
    }
}