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

        private static AssignRecordCollection records;

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
            records.AddRecord(toLog.path, toLog.value);
        }

        public static void RecordExecutedExpression(AssignLogInfo toLog)
        {
            records.MarkExecutedRecord(toLog.path, toLog.value);
        }

        public static void RecordConverter(string converter)
        {
            records.AddConverterToRecord(converter);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }
    }
}