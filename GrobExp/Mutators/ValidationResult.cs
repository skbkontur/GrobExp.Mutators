using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators
{
    public class ValidationResult
    {
        public ValidationResult(ValidationResultType validationResultType, MultiLanguageTextBase message)
        {
            Type = validationResultType;
            Message = message;
        }

        public static ValidationResult Warning(MultiLanguageTextBase message)
        {
            return new ValidationResult(ValidationResultType.Warning, message);
        }

        public static ValidationResult Error(MultiLanguageTextBase message)
        {
            return new ValidationResult(ValidationResultType.Error, message);
        }

        public override string ToString()
        {
            switch (Type)
            {
            // For performance issues
            case ValidationResultType.Ok:
                return "Ok";
            case ValidationResultType.Error:
                return "Error";
            case ValidationResultType.Warning:
                return "Warning";
            default:
                return "Unknown";
            }
        }

        public ValidationResultType Type { get; private set; }
        public MultiLanguageTextBase Message { get; private set; }

        public static readonly ValidationResult Ok = new ValidationResult(ValidationResultType.Ok, null);
    }
}