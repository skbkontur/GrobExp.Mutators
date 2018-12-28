namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    internal class ValidationLogInfo
    {
        public ValidationLogInfo(string name, string condition)
        {
            Name = name;
            Condition = condition;
        }

        public string Name { get; }
        public string Condition { get; }
    }
}