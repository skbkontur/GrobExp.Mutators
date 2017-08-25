using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording
{
    public class RecordNode
    {
        public RecordNode(string name, string parentFullName, bool isExcludedFromCovreage = false)
        {
            Name = name;
            Records = new List<RecordNode>();
            CompiledCount = 1;
            FullName = string.IsNullOrEmpty(parentFullName) ? name : parentFullName + "." + name;
            IsExcludedFromCoverage = isExcludedFromCovreage;
        }

        public List<RecordNode> Records { get; private set; }
        public int CompiledCount { get; private set; }
        public int ExecutedCount { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public bool IsExcludedFromCoverage { get; set; }

        private bool ContainsRecord(string recordName)
        {
            return Records.Any(node => node.Name == recordName);
        }

        private RecordNode GetRecordByName(string recordName)
        {
            return Records.FirstOrDefault(record => record.Name == recordName);
        }

        public void RecordCompilingExpression(List<string> pathComponents, string value, bool isExcludedFromCoverage = false)
        {
            CompiledCount++;

            var recordName = pathComponents[0];
            RecordNode node;
            if(ContainsRecord(recordName))
                node = GetRecordByName(recordName);
            else
            {
                node = new RecordNode(recordName, FullName, isExcludedFromCoverage);
                Records.Add(node);
            }

            if(pathComponents.Count == 1)
                node.RecordCompilingExpression(value, isExcludedFromCoverage);
            else
                node.RecordCompilingExpression(pathComponents.GetRange(1, pathComponents.Count - 1), value, isExcludedFromCoverage);
        }

        private void RecordCompilingExpression(string value, bool isExcludedFromCoverage = false)
        {
            CompiledCount++;
            if (!ContainsRecord(value))
                Records.Add(new RecordNode(value, FullName, isExcludedFromCoverage));
        }

        private void RecordExecutingExpression(string value)
        {
            ExecutedCount++;
            if(!ContainsRecord(value))
                RecordCompilingExpression(value);
            GetRecordByName(value).ExecutedCount++;
        }

        public void RecordExecutingExpression(List<string> pathComponents, string value)
        {
            ExecutedCount++;

            var recordName = pathComponents[0];
            if(!ContainsRecord(recordName))
                RecordCompilingExpression(pathComponents, value);  

            var node = GetRecordByName(recordName);
            if (pathComponents.Count == 1)
                node.RecordExecutingExpression(value);
            else
                node.RecordExecutingExpression(pathComponents.GetRange(1, pathComponents.Count - 1), value);
        }
    }
}