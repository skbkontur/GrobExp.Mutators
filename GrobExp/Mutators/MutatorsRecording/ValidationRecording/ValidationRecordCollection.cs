using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public class ValidationRecordCollection
    {
        public ValidationRecordCollection()
        {
            validatorRecords = new List<RecordNode>();
            errorValidationRecords = new List<RecordNode>();
        }

        public void AddValidatorToRecord(string validatorName)
        {
            AddValidatorToRecord(validatorRecords, ref currentValidator, validatorName);
            AddValidatorToRecord(errorValidationRecords, ref currentErrorValidator, validatorName);
        }

        private void AddValidatorToRecord(List<RecordNode> records, ref RecordNode validator, string validatorName)
        {
            validator = records.FirstOrDefault(v => v.Name == validatorName);
            if (validator != null)
                return;
            validator = new RecordNode(validatorName, "");
            records.Add(validator);
        }

        public void ResetCurrentValidator()
        {
            currentValidator = null;
            currentErrorValidator = null;
        }

        public void RecordCompilingValidation(ValidationLogInfo validationInfo)
        {
            if(currentValidator != null)
            {
                currentValidator.RecordCompilingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, "Ok");
                currentValidator.RecordCompilingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, "Error");
            }

            if(currentErrorValidator != null)
                currentErrorValidator.RecordCompilingExpression(new List<string> {validationInfo.Name, validationInfo.Condition}, "Error");
        }

        public void RecordExecutingValidation(ValidationLogInfo validationInfo, string validationResult)
        {
            if(currentValidator != null)
                currentValidator.RecordExecutingExpression(new List<string>{validationInfo.Name, validationInfo.Condition}, validationResult);

            if(currentErrorValidator != null && validationResult == "Error")
                currentErrorValidator.RecordExecutingExpression(new List<string>{validationInfo.Name, validationInfo.Condition}, validationResult);
        }

        public List<RecordNode> GetRecords()
        {
            return validatorRecords;
        }

        public List<RecordNode> GetErrorRecords()
        {
            return errorValidationRecords;
        } 

        private readonly List<RecordNode> validatorRecords;
        private RecordNode currentValidator;
        private readonly List<RecordNode> errorValidationRecords;
        private RecordNode currentErrorValidator;
    }
}