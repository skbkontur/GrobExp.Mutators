using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public class AssignRecordCollection
    {
        public void RecordCompilingExpression(Type converterType, string path, string value, bool isExcludedFromCoverage = false)
        {
            converterRecords.GetOrAdd(converterType, RecordNode.Create).RecordCompilingExpression(path.Split('.').ToList(), value, isExcludedFromCoverage);
        }

        public void RecordExecutingExpression(Type converterType, string path, string value, Lazy<bool> isExcludedFromCoverage = null)
        {
            converterRecords.GetOrAdd(converterType, RecordNode.Create).RecordExecutingExpression(path.Split('.').ToList(), value, isExcludedFromCoverage);
        }

        public List<RecordNode> GetRecords()
        {
            return converterRecords.Select(x => x.Value).ToList();
        }

        private readonly ConcurrentDictionary<Type, RecordNode> converterRecords = new ConcurrentDictionary<Type, RecordNode>();
    }
}