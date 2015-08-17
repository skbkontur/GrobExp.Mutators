using System;
using System.Collections.Generic;

namespace GrobExp.Mutators.AssignRecording
{
    internal class MutatorsAssignRecorder : IMutatorsAssignRecorder
    {
        public MutatorsAssignRecorder()
        {
            records = new AssignRecordCollection();
        }

        public List<RecordNode> GetRecords()
        {
            return records.GetRecords();
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
            instance.records.AddRecord(toLog.path.ToString(), toLog.value.ToString());
        }

        public static void RecordExecutedExpression(AssignLogInfo toLog)
        {
            instance.records.MarkExecutedRecord(toLog.path.ToString(), toLog.value.ToString());
        }

        public static void RecordConverter(string converter)
        {
            instance.records.AddConverterToRecord(converter);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }

        [ThreadStatic]
        private static MutatorsAssignRecorder instance;
        private readonly AssignRecordCollection records;
    }
}