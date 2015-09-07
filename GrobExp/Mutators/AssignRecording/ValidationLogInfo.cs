namespace GrobExp.Mutators.AssignRecording
{
    public class ValidationLogInfo
    {
        public string Name { get; private set; }
        public string Condition { get; private set; }
        public string Result { get; private set; }

        public ValidationLogInfo(string name, string condition, string result)
        {
            Name = name;
            Condition = condition;
            Result = result;
        }
    }
}