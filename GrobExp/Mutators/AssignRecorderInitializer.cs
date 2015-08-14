namespace GrobExp.Mutators
{
    public class AssignRecorderInitializer
    {
        public static IMutatorsAssignRecorder StartAssignRecorder()
        {
            MutatorsAssignRecorder.StartRecording();
            return MutatorsAssignRecorder.Instance;
        }
    }
}