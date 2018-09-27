using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public class AssignRecordCollection
    {
        public void AddConverterToRecord(string converterName)
        {
            converterRecords.GetOrAdd(converterName, name => new RecordNode(name, ""));
        }

        public void ResetCurrentConvertor()
        {
            currentConverterRecord = null;
        }

        public void RecordCompilingExpression(string path, string value, bool isExcludedFromCoverage = false)
        {
            currentConverterRecord?.RecordCompilingExpression(path.Split('.').ToList(), value, isExcludedFromCoverage);
        }

        public void RecordExecutingExpression(string path, string value, Lazy<bool> isExcludedFromCoverage = null)
        {
            currentConverterRecord?.RecordExecutingExpression(path.Split('.').ToList(), value, isExcludedFromCoverage);
        }

        public List<RecordNode> GetRecords()
        {
            return converterRecords.Select(x => x.Value).ToList();
        }

        //private readonly List<RecordNode> converterRecords;
        private RecordNode currentConverterRecord;
        private readonly ConcurrentDictionary<string, RecordNode> converterRecords = new ConcurrentDictionary<string, RecordNode>();
    }
}