using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.AssignRecording
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
            currentValidator.RecordCompilingExpression(new List<string>{validation.Name, validation.Condition}, validation.Result);
        }

        public void RecordExecutingValidation(ValidationLogInfo validation)
        {
            currentValidator.RecordExecutingExpression(new List<string>{validation.Name, validation.Condition}, validation.Result);
        }

        public List<RecordNode> GetRecords()
        {
            return validations;
        } 

        private readonly List<RecordNode> validations;
        private RecordNode currentValidator;
    }
}