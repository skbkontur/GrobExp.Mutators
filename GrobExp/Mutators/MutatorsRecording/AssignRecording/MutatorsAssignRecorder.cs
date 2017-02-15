using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    internal class MutatorsAssignRecorder : IMutatorsAssignRecorder
    {
        public MutatorsAssignRecorder()
        {
            recordsCollection = new AssignRecordCollection();
        }

        public List<RecordNode> GetRecords()
        {
            return recordsCollection.GetRecords();
        }

        public void Stop()
        {
            instance = null;
        }

        public static MutatorsAssignRecorder StartRecording()
        {
            return instance ?? (instance = new MutatorsAssignRecorder());
        }

        public static void RecordCompilingExpression(AssignLogInfo toLog)
        {
            instance.recordsCollection.RecordCompilingExpression(toLog.Path.ToString(), toLog.Value.ToString());
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
    }
}