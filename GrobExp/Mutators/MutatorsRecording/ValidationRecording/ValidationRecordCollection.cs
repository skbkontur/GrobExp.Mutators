using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public class ValidationRecordCollection
    {
        public ValidationRecordCollection()
        {
            validatorRecords = new List<RecordNode>();
        }

        public void AddValidatorToRecord(string validatorName)
        {
            currentValidator = validatorRecords.FirstOrDefault(validator => validator.Name == validatorName);
            if(currentValidator != null)
                return;
            currentValidator = new RecordNode(validatorName, "");
            validatorRecords.Add(currentValidator);
        }

        public void ResetCurrentValidator()
        {
            currentValidator = null;
        }

        public void RecordCompilingValidation(ValidationLogInfo validationInfo)
        {
            if(currentValidator == null) return;
            currentValidator.RecordCompilingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, "Ok");
            currentValidator.RecordCompilingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, "Error");
        }

        public void RecordExecutingValidation(ValidationLogInfo validationInfo, string validationResult)
        {
            if(currentValidator != null)
                currentValidator.RecordExecutingExpression(new List<string>{validationInfo.Name, validationInfo.Condition}, validationResult);
        }

        public List<RecordNode> GetRecords()
        {
            return validatorRecords;
        } 

        private readonly List<RecordNode> validatorRecords;
        private RecordNode currentValidator;
    }
}