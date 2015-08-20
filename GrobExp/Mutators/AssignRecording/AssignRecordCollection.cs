using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.AssignRecording
{
    public class AssignRecordCollection
    {
        public AssignRecordCollection()
        {
            converterRecords = new List<RecordNode>();
        }

        public void AddConverterToRecord(string converterName)
        {
            currentConverterRecord = converterRecords.FirstOrDefault(converter => converter.Name == converterName);
            if(currentConverterRecord != null) return;
            currentConverterRecord = new RecordNode(converterName);
            converterRecords.Add(currentConverterRecord);
        }

        public void ResetCurrentConvertor()
        {
            currentConverterRecord = null;
        }

        public void RecordCompilingExpression(string path, string value)
        {
            if(currentConverterRecord != null)
                currentConverterRecord.RecordCompilingExpression(path, value);
        }

        public void RecordExecutingExpression(string path, string value)
        {
            if(currentConverterRecord != null)
                currentConverterRecord.RecordExecutingExpression(path, value);
        }

        public List<RecordNode> GetRecords()
        {
            return converterRecords;
        }

        private readonly List<RecordNode> converterRecords;
        private RecordNode currentConverterRecord;
    }
}