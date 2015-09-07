namespace GrobExp.Mutators.AssignRecording
{
    public class ValidationRecorderInitializer
    {
        public static IMutatorsValidationRecorder StartValidationRecorder()
        {
            return MutatorsValidationRecorder.StartRecording();
        }
    }
}