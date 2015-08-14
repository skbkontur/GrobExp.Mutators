using System;
using System.Collections.Generic;

using GrobExp.Mutators.AssignRecording;

namespace GrobExp.Mutators
{
    public interface IMutatorsAssignRecorder
    {
        List<RecordNode> GetNotCoveredRecords();
        void Stop();
    }

    internal class MutatorsAssignRecorder : IMutatorsAssignRecorder
    {
        [ThreadStatic]
        private static MutatorsAssignRecorder instance;

        private static AssignRecordCollection notCoveredRecords;

        public MutatorsAssignRecorder()
        {
            notCoveredRecords = new AssignRecordCollection();
        }

        public static MutatorsAssignRecorder Instance { get { return instance; } }

        public List<RecordNode> GetNotCoveredRecords()
        {
            return notCoveredRecords.GetRecords();
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
            notCoveredRecords.AddRecord(toLog.path, toLog.value);
        }

        public static void RecordExecutedExpression(AssignLogInfo toLog)
        {
            notCoveredRecords.MarkExecutedRecord(toLog.path, toLog.value);
        }

        public static void RecordConverter(string converter)
        {
            notCoveredRecords.AddConverterToRecord(converter);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }
    }
}