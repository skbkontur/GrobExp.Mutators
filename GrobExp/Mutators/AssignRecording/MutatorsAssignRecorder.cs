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

        public static void RecordCompilingExpression(AssignLogInfo toLog)
        {
            instance.recordsCollection.RecordCompilingExpression(toLog.Path.ToString(), toLog.Value.ToString());
        }

        public static void RecordExecutingExpression(AssignLogInfo toLog)
        {
            instance.recordsCollection.RecordExecutingExpression(toLog.Path.ToString(), toLog.Value.ToString());
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