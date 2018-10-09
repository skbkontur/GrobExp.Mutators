using System;
using System.Collections.Generic;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    internal class MutatorsValidationRecorder : IMutatorsValidationRecorder
    {
        public MutatorsValidationRecorder()
        {
            recordsCollection = new ValidationRecordCollection();
        }

        public List<RecordNode> GetErrorRecords()
        {
            return recordsCollection.GetErrorRecords();
        }

        public void Stop()
        {
            instance = null;
        }

        public static MutatorsValidationRecorder StartRecording()
        {
            return instance ?? (instance = new MutatorsValidationRecorder());
        }

        public static void RecordCompilingValidation(Type converterType, ValidationLogInfo validationInfo)
        {
            instance.recordsCollection.RecordCompilingValidation(converterType, validationInfo);
        }

        public static void RecordExecutingValidation(Type converterType, ValidationLogInfo validationInfo, string validationResult)
        {
            if (IsRecording())
                instance.recordsCollection.RecordExecutingValidation(converterType, validationInfo, validationResult);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }

        private static MutatorsValidationRecorder instance;

        private readonly ValidationRecordCollection recordsCollection;
    }
}