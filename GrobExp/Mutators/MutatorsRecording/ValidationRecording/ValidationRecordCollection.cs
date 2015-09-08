using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.MutatorsRecording.ValidationRecording
{
    public class ValidationRecordCollection
    {
        public ValidationRecordCollection()
        {
            validations = new List<RecordNode>();
        }

        public void AddValidatorToRecord(string validatorName)
        {
            currentValidator = validations.FirstOrDefault(validator => validator.Name == validatorName);
            if(currentValidator != null)
                return;
            currentValidator = new RecordNode(validatorName, "");
            validations.Add(currentValidator);
        }

        public void ResetCurrentValidator()
        {
            currentValidator = null;
        }

        public void RecordCompilingValidation(ValidationLogInfo validation)
        {
            currentValidator.RecordCompilingExpression(new List<string>{validation.Name, validation.Condition}, "Ok");
            currentValidator.RecordCompilingExpression(new List<string>{validation.Name, validation.Condition}, "Error");
        }

        public void RecordExecutingValidation(ValidationLogInfo validation, string validationResult)
        {
            currentValidator.RecordExecutingExpression(new List<string>{validation.Name, validation.Condition}, validationResult);
        }

        public List<RecordNode> GetRecords()
        {
            return validations;
        } 

        private readonly List<RecordNode> validations;
        private RecordNode currentValidator;
    }
}