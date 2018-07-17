using System;

using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators
{
    public class FormattedValidationResult : IComparable<FormattedValidationResult>, IComparable
    {
        public FormattedValidationResult(ValidationResult validationResult, object value, MultiLanguagePathText path, int priority = 0)
        {
            Priority = priority;
            Type = validationResult.Type;
            Message = validationResult.Message;
            var multiLanguageTextBaseWithPath = Message as MultiLanguageTextBaseWithPath;
            if (multiLanguageTextBaseWithPath != null)
            {
                multiLanguageTextBaseWithPath.Path = path;
                multiLanguageTextBaseWithPath.Value = value;
            }
            else
            {
                Path = path;
                Value = value;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            if (!(obj is FormattedValidationResult))
                throw new ArgumentException();
            return CompareTo((FormattedValidationResult)obj);
        }

        public int CompareTo(FormattedValidationResult other)
        {
            if (other == null)
                return 1;
            if (ReferenceEquals(this, other)) return 0;
            var result = ((int)Type).CompareTo((int)other.Type);
            if (result != 0)
                return -result;
            result = (other.Priority).CompareTo(Priority);
            if (result != 0)
                return result;
            var path = GetPath();
            var otherPath = other.GetPath();
            result = path == null ? (otherPath == null ? 0 : -1) : path.CompareTo(otherPath);
            if (result != 0)
                return result;
            return String.Compare((Message == null ? "" : Message.GetText("RU")), other.Message == null ? "" : other.Message.GetText("RU"), StringComparison.InvariantCultureIgnoreCase);
        }

        public static FormattedValidationResult Ok(object value, MultiLanguagePathText path, int priority = 0)
        {
            return new FormattedValidationResult(ValidationResult.Ok, value, path, priority);
        }

        public static FormattedValidationResult Error(MultiLanguageTextBase message, object value, MultiLanguagePathText path, int priority = 0)
        {
            return new FormattedValidationResult(ValidationResult.Error(message), value, path, priority);
        }

        public int Priority { get; set; }

        public ValidationResultType Type { get; set; }

        public MultiLanguageTextBase Message { get; set; }

        public MultiLanguagePathText Path { get; set; }

        public object Value { get; set; }

        private MultiLanguagePathText GetPath()
        {
            return Message is MultiLanguageTextBaseWithPath ? ((MultiLanguageTextBaseWithPath)Message).Path : Path;
        }
    }
}