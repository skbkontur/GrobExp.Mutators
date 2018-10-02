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
            IsExcludedFromCoverage = isExcludedFromCovreage;
        }

        public RecordNode RecordCompilingExpression(List<string> pathComponents, string value, bool isExcludedFromCoverage = false)
        {
            Interlocked.Increment(ref compiledCount);
            if (!isExcludedFromCoverage)
                IsExcludedFromCoverage = false;

            var recordName = pathComponents[0];
            var node = Records.GetOrAdd(recordName, name => new RecordNode(name, FullName, isExcludedFromCoverage));
            if (pathComponents.Count == 1)
                node.RecordCompilingExpression(value, isExcludedFromCoverage);
            else
                node.RecordCompilingExpression(pathComponents.GetRange(1, pathComponents.Count - 1), value, isExcludedFromCoverage);
            return node;
        }

        private RecordNode GetCompilingExpression(string value, bool isExcludedFromCoverage = false)
        {
            Interlocked.Increment(ref compiledCount);
            if (!isExcludedFromCoverage)
                IsExcludedFromCoverage = false;
            return new RecordNode(value, FullName, isExcludedFromCoverage);
        }

        private void RecordCompilingExpression(string value, bool isExcludedFromCoverage = false)
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
        public bool IsExcludedFromCoverage { get; private set; }

        private int compiledCount;
        private int executedCount;
    }
}