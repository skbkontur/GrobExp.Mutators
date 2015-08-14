using System;
using System.Collections.Generic;

using GrobExp.Mutators.AssignRecording;

namespace GrobExp.Mutators
{
    public interface IMutatorsAssignRecorder
    {
        List<RecordNode> GetRecords();
        void Stop();
    }

    internal class MutatorsAssignRecorder : IMutatorsAssignRecorder
    {
        [ThreadStatic]
        private static MutatorsAssignRecorder instance;

        private AssignRecordCollection records;

        public MutatorsAssignRecorder()
        {
            records = new AssignRecordCollection();
        }

        public static MutatorsAssignRecorder Instance { get { return instance; } }

        public List<RecordNode> GetRecords()
        {
            return records.GetRecords();
        }

        public void Stop()
        {
            instance = null;
        }

        public static void StartRecording()
        {
            if(instance == null)
                instance = new MutatorsAssignRecorder();
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
    }
}