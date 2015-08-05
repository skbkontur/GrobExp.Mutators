using System;
using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators
{
    public interface IMutatorsAssignRecorder
    {
        List<string> GetCompiledRecords();
        List<string> GetExecutedRecords();
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

        public MutatorsAssignRecorder()
        {
            compiledRecords = new List<string>();
            executedRecords = new List<string>();
        }


        public List<string> GetCompiledRecords()
        {
            return compiledRecords.Distinct().ToList();
        }

        public List<string> GetExecutedRecords()
        {
            return executedRecords.Distinct().ToList();
        }

        public void Stop()
        {
            instance = null;
        }

        public static void RecordCompiledExpression(params object[] toLog)
        {
                compiledRecords.Add(String.Join(" ", toLog));
        }

        public static void RecordExecutedExpression(object toLog)
        {
            if(toLog != null)
               executedRecords.Add(toLog.ToString());
        }
    }
}