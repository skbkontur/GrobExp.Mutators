using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public class ValidationRecordCollection
    {
        public void RecordCompilingValidation(Type validatorType, ValidationLogInfo validationInfo)
        {
            errorValidations.GetOrAdd(validatorType, RecordNode.Create).RecordCompilingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, "Error");
        }

        public void RecordExecutingValidation(Type validatorType, ValidationLogInfo validationInfo, string validationResult)
        {
            errorValidations.GetOrAdd(validatorType, RecordNode.Create).RecordExecutingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, validationResult);
        }

        public List<RecordNode> GetErrorRecords()
        {
            return errorValidations.Select(x => x.Value).ToList();
        }

        private readonly ConcurrentDictionary<Type, RecordNode> errorValidations = new ConcurrentDictionary<Type, RecordNode>();
    }
}