using System;
using System.Collections.Generic;

namespace GrobExp.Mutators.AssignRecording
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

        public static void RecordCompiledExpression(AssignLogInfo toLog)
        {
            instance.recordsCollection.RecordCompiledExpression(toLog.path.ToString(), toLog.value.ToString());
        }

        public static void RecordExecutedExpression(AssignLogInfo toLog)
        {
            instance.recordsCollection.RecordExecutedExpression(toLog.path.ToString(), toLog.value.ToString());
        }

        public static void RecordConverter(string converter)
        {
            instance.recordsCollection.AddConverterToRecord(converter);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }

        [ThreadStatic]
        private static MutatorsAssignRecorder instance;
        private readonly AssignRecordCollection recordsCollection;
    }
}