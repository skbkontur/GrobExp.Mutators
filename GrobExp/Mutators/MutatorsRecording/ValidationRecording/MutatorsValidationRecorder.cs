using System;
using System.Collections.Generic;

using GrobExp.Mutators.MutatorsRecording.AssignRecording;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    internal class MutatorsValidationRecorder : IMutatorsValidationRecorder
    {
        public MutatorsValidationRecorder()
        {
            recordsCollection = new ValidationRecordCollection();
        }

        public List<RecordNode> GetRecords()
        {
            return recordsCollection.GetRecords();
        }

        public void Stop()
        {
            instance = null;
        }

        public static MutatorsValidationRecorder StartRecording()
        {
            return instance ?? (instance = new MutatorsValidationRecorder());
        }

        public static void RecordCompilingValidation(ValidationLogInfo toLog)
        {
            instance.recordsCollection.RecordCompilingValidation(toLog);
        }


        public static void RecordExecutingValidation(ValidationLogInfo toLog, string validationResult)
        {
            if(IsRecording())
                instance.recordsCollection.RecordExecutingValidation(toLog, validationResult);
        }

        public static void AddValidatorToRecord(string validatorName)
        {
            instance.recordsCollection.AddValidatorToRecord(validatorName);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }

        [ThreadStatic]
        private static MutatorsValidationRecorder instance;
        private readonly ValidationRecordCollection recordsCollection;  
    }
}