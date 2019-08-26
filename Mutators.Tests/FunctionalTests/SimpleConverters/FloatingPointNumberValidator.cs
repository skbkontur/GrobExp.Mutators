using System.Linq;

using GrobExp.Mutators;
using GrobExp.Mutators.Validators.Texts;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class FloatingPointNumberValidator
    {
        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ValidationResult.Ok;
            var errorResult = ValidationResult.Error(new ValueRequiredText());
            if (value[0] == '-' || value[0] == '+')
                value = value.Remove(0, 1);
            if (string.IsNullOrEmpty(value))
                return errorResult;
            value = value.Trim('0');
            var parts = value.Split('.');
            var ordinalPart = parts[0];
            if (string.IsNullOrEmpty(ordinalPart))
                ordinalPart = "0";
            var fractionalPart = parts.Length == 2 ? parts[1] : "";
            if (parts.Length > 2 || ordinalPart.Length + fractionalPart.Length > maxLength || !ordinalPart.All(char.IsDigit) || !fractionalPart.All(char.IsDigit))
                return errorResult;
            return ValidationResult.Ok;
        }

        private const int maxLength = 17;
    }
}