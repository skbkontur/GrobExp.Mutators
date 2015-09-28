namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public class ValidationRecorderInitializer
    {
        public static IMutatorsValidationRecorder StartValidationRecorder()
        {
            return MutatorsValidationRecorder.StartRecording();
        }
    }
}