using System;
using System.Collections.Generic;
using System.Linq;

using GrobExp.Mutators.AssignRecording;

namespace GrobExp.Mutators
{
    public interface IMutatorsAssignRecorder
    {
        List<string> GetCompiledRecords();
        List<string> GetExecutedRecords();
        List<RecordNode> GetNotCoveredRecords();
        void Stop();
    }

    internal class MutatorsAssignRecorder : IMutatorsAssignRecorder
    {
        [ThreadStatic]
        private static MutatorsAssignRecorder instance;

        public static void StartRecording()
        {
            if (instance == null)
                instance = new MutatorsAssignRecorder();
        }

        public static MutatorsAssignRecorder Instance
        {
            get { return instance; }
        }

        private static List<string> compiledRecords;
        private static List<string> executedRecords;
        private static AssignRecordCollection notCoveredRecords;

        public MutatorsAssignRecorder()
        {
            compiledRecords = new List<string>();
            executedRecords = new List<string>();
            notCoveredRecords = new AssignRecordCollection();
        }


        public List<string> GetCompiledRecords()
        {
            return compiledRecords.Distinct().ToList();
        }

        public List<string> GetExecutedRecords()
        {
            return executedRecords.Distinct().ToList();
        }


        public List<RecordNode> GetNotCoveredRecords()
        {
            return notCoveredRecords.GetRecords();
        }

        public void Stop()
        {
            instance = null;
        }

        public static void RecordCompiledExpression(string toLog)
        {
            compiledRecords.Add(String.Join(" ", toLog));
            var pathValue = toLog.Split(new []{'='}, 2);
            notCoveredRecords.AddRecord(pathValue[0], pathValue[1]);
        }

        public static void RecordExecutedExpression(string toLog)
        {
            if(toLog != null)
               executedRecords.Add(toLog);
            var pathValue = toLog.Split(new[] { '=' }, 2);
            notCoveredRecords.ExtractRecord(pathValue[0], pathValue[1]);
        }

        public static void RecordConverter(string converter)
        {
            notCoveredRecords.AddConverterToRecord(converter);
            compiledRecords.Add("Converter: " + converter);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }
    }
}