using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public class AssignRecordCollection
    {
        public void AddConverterToRecord(Type converterType)
        {
            converterRecords.TryAdd(converterType, new RecordNode(converterType.Name, ""));
        }

        public void ResetCurrentConvertor()
        {
        }

        public void RecordCompilingExpression(Type converterType, string path, string value, bool isExcludedFromCoverage = false)
        {
            if (converterRecords.TryGetValue(converterType, out var converterRecord))
                converterRecord.RecordCompilingExpression(path.Split('.').ToList(), value, isExcludedFromCoverage ? 1 : 0);
        }

        public void RecordExecutingExpression(Type converterType, string path, string value, Lazy<bool> isExcludedFromCoverage = null)
        {
            if (converterRecords.TryGetValue(converterType, out var converterRecord))
                converterRecord.RecordExecutingExpression(path.Split('.').ToList(), value, isExcludedFromCoverage);
        }

        public List<RecordNode> GetRecords()
        {
            return converterRecords.Select(x => x.Value).ToList();
        }

        private readonly ConcurrentDictionary<Type, RecordNode> converterRecords = new ConcurrentDictionary<Type, RecordNode>();
    }
}