using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GrobExp.Mutators.MutatorsRecording
{
    public class RecordNode
    {
        public RecordNode(string name, string parentFullName, bool isExcludedFromCovreage = false)
        {
            Name = name;
            compiledCount = 1;
            FullName = string.IsNullOrEmpty(parentFullName) ? name : parentFullName + "." + name;
            excludedFromCoverage = isExcludedFromCovreage ? 1 : 0;
        }

        public RecordNode RecordCompilingExpression(List<string> pathComponents, string value, bool isExcludedFromCoverage = false)
        {
            var excluded = isExcludedFromCoverage ? 1 : 0;
            Interlocked.Increment(ref compiledCount);
            Interlocked.CompareExchange(ref excludedFromCoverage, excluded, 0);

            var recordName = pathComponents[0];
            var node = Records.GetOrAdd(recordName, name => new RecordNode(name, FullName, isExcludedFromCoverage));
            if (pathComponents.Count == 1)
                node.RecordCompilingExpression(value, excluded);
            else
                node.RecordCompilingExpression(pathComponents.GetRange(1, pathComponents.Count - 1), value, isExcludedFromCoverage);
            return node;
        }

        private RecordNode GetCompilingExpression(string value, int isExcludedFromCoverage = 0)
        {
            Interlocked.Increment(ref compiledCount);
            Interlocked.CompareExchange(ref excludedFromCoverage, isExcludedFromCoverage, 0);
            return new RecordNode(value, FullName, isExcludedFromCoverage == 1);
        }

        private void RecordCompilingExpression(string value, int isExcludedFromCoverage = 0)
        {
            var record = GetCompilingExpression(value, isExcludedFromCoverage);
            Records.TryAdd(value, record);
        }

        private void RecordExecutingExpression(string value)
        {
            Interlocked.Increment(ref executedCount);
            var node = Records.GetOrAdd(value, name => GetCompilingExpression(name));
            Interlocked.Increment(ref node.executedCount);
        }

        public void RecordExecutingExpression(List<string> pathComponents, string value, Lazy<bool> isExcludedFromCoverage = null)
        {
            Interlocked.Increment(ref executedCount);

            var recordName = pathComponents[0];
            var node = Records.GetOrAdd(recordName, name => RecordCompilingExpression(pathComponents, value, isExcludedFromCoverage?.Value ?? false));
            if (pathComponents.Count == 1)
                node.RecordExecutingExpression(value);
            else
                node.RecordExecutingExpression(pathComponents.GetRange(1, pathComponents.Count - 1), value, isExcludedFromCoverage);
        }

        public ConcurrentDictionary<string, RecordNode> Records { get; } = new ConcurrentDictionary<string, RecordNode>();
        public int CompiledCount => compiledCount;
        public int ExecutedCount => executedCount;
        public string Name { get; }
        public string FullName { get; }
        public bool IsExcludedFromCoverage => excludedFromCoverage != 0;

        private int compiledCount;
        private int executedCount;
        private int excludedFromCoverage;
    }
}