using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public class ValidationRecordCollection
    {
        public void AddValidatorToRecord(Type validatorType)
        {
            errorValidations.TryAdd(validatorType, new RecordNode(validatorType.ToString(), ""));
        }

        public void RecordCompilingValidation(Type validatorType, ValidationLogInfo validationInfo)
        {
            if (errorValidations.TryGetValue(validatorType, out var errorValidator))
                errorValidator.RecordCompilingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, "Error");
        }

        public void RecordExecutingValidation(Type validatorType, ValidationLogInfo validationInfo, string validationResult)
        {
            if (errorValidations.TryGetValue(validatorType, out var errorValidator) && validationResult == "Error")
                errorValidator.RecordExecutingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, validationResult);
        }

        public List<RecordNode> GetErrorRecords()
        {
            return errorValidations.Select(x => x.Value).ToList();
        }

        private readonly ConcurrentDictionary<Type, RecordNode> errorValidations = new ConcurrentDictionary<Type, RecordNode>();
    }
}