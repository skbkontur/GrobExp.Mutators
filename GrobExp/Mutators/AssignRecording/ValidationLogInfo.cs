namespace GrobExp.Mutators.AssignRecording
{
    public class ValidationLogInfo
    {
        public string Name { get; private set; }
        public string Condition { get; private set; }

        public ValidationLogInfo(string name, string condition)
        {
            Name = name;
            Condition = condition;
        }
    }
}