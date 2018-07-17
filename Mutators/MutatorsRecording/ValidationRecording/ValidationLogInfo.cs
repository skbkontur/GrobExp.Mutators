namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public class ValidationLogInfo
    {
        public ValidationLogInfo(string name, string condition)
        {
            Name = name;
            Condition = condition;
        }

        public string Name { get; private set; }
        public string Condition { get; private set; }
    }
}