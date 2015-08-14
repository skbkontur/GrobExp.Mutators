using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.AssignRecording
{
    public class RecordNode
    {
        public RecordNode(string name)
        {
            Name = name;
            Records = new List<RecordNode>();
            CompiledCount = 1;
        }

        public List<RecordNode> Records { get; private set; }
        public int CompiledCount { get; private set; }
        public int ExecutedCount { get; private set; }
        public string Name { get; private set; }

        private bool ContainsRecord(string recordName)
        {
            return Records.Select(r => r.Name).Contains(recordName);
        }

        private RecordNode GetRecordByName(string recordName)
        {
            return Records.FirstOrDefault(record => record.Name == recordName);
        }

        public void AddRecord(string path, string value)
        {
            CompiledCount++;
            var dividedPath = path.Split(new[] {'.'}, 2);

            var recordName = dividedPath[0];
            RecordNode node;
            if(ContainsRecord(recordName))
                node = GetRecordByName(recordName);
            else
            {
                node = new RecordNode(recordName);
                Records.Add(node);
            }

            if(dividedPath.Count() == 1)
                node.AddRecord(value);
            else
                node.AddRecord(dividedPath[1], value);
        }

        private void AddRecord(string value)
        {
            CompiledCount++;
            if (!ContainsRecord(value))
                Records.Add(new RecordNode(value));
        }

        private void MarkExecutedRecord(string value)
        {
            ExecutedCount++;
            if(ContainsRecord(value))
                GetRecordByName(value).ExecutedCount++;
        }

        public void MarkExecutedRecord(string path, string value)
        {
            ExecutedCount++;
            var dividedPath = path.Split(new[] {'.'}, 2);
            var recordName = dividedPath[0];
            if(!ContainsRecord(recordName))
                return;

            var node = GetRecordByName(recordName);
            if(dividedPath.Count() == 1)
                node.MarkExecutedRecord(value);
            else
                node.MarkExecutedRecord(dividedPath[1], value);
        }
    }
}