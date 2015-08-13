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

        public static void RecordCompiledExpression(string toLog)
        {
            var pathValue = toLog.Split(new[] {'='}, 2);
            notCoveredRecords.AddRecord(pathValue[0], pathValue[1]);
        }

        public static void RecordExecutedExpression(string toLog)
        {
            var pathValue = toLog.Split(new[] {'='}, 2);
            notCoveredRecords.ExtractRecord(pathValue[0], pathValue[1]);
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