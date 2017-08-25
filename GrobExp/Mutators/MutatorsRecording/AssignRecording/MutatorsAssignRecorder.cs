using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    internal class MutatorsAssignRecorder : IMutatorsAssignRecorder
    {
        public MutatorsAssignRecorder(Type[] excludedFromCoverage)
        {
            recordsCollection = new AssignRecordCollection();
            this.excludedFromCoverage = excludedFromCoverage;
        }

        public List<RecordNode> GetRecords()
        {
            return recordsCollection.GetRecords();
        }

        public void Stop()
        {
            instance = null;
        }

        public static MutatorsAssignRecorder StartRecording(Type[] excludedFromCoverage)
        {
            return instance ?? (instance = new MutatorsAssignRecorder(excludedFromCoverage ?? new Type[0]));
        }

        public static void RecordCompilingExpression(AssignLogInfo toLog)
        {
            var chainsOfPath = toLog.Path.SmashToSmithereens().Select(x => x.Type).ToArray();
            var chainsOfValue = toLog.Value.SmashToSmithereens().Select(x => x.Type).ToArray();
            var isExcluded = instance.excludedFromCoverage.Any(x => chainsOfPath.Contains(x) || chainsOfValue.Contains(x));

            instance.recordsCollection.RecordCompilingExpression(toLog.Path.ToString(), toLog.Value.ToString(), isExcluded);
        }

        public static void RecordExecutingExpression(AssignLogInfo toLog)
        {
            if(IsRecording())
                instance.recordsCollection.RecordExecutingExpression(toLog.Path.ToString(), toLog.Value.ToString());
        }

        public static void RecordExecutingExpressionWithValueObjectCheck(AssignLogInfo toLog, object executedValue)
        {
            if(executedValue != null)
                RecordExecutingExpression(toLog);
        }

        public static void RecordExecutingExpressionWithNullableValueCheck<T>(AssignLogInfo toLog, T? executedValue) where T : struct 
        {
            if (executedValue != null || toLog.Value.NodeType == ExpressionType.Constant && toLog.Value.ToConstant().Value == null)
                RecordExecutingExpression(toLog);
        }

        public static void RecordConverter(string converter)
        {
            instance.recordsCollection.AddConverterToRecord(converter);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }

        public static void StopRecordingConverter()
        {
            instance.recordsCollection.ResetCurrentConvertor();
        }

        [ThreadStatic]
        private static MutatorsAssignRecorder instance;
        private readonly AssignRecordCollection recordsCollection;
        private readonly Type[] excludedFromCoverage;
    }
}