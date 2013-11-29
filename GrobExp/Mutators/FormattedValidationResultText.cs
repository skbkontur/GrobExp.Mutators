using System;
using System.Linq;

using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators
{
    [MultiLanguageTextType("FormattedValidationResultText")]
    public class FormattedValidationResultText : MultiLanguageTextBase
    {
        public new string GetText(string language)
        {
            return base.GetText(language);
        }

        public FormattedValidationResult[] ValidationResults { get; set; }

        protected override void Register()
        {
            Register("RU");
            Register("EN");
        }

        private void Register(string language)
        {
            Register(language, () =>
                               string.Join(Environment.NewLine, (ValidationResults ?? new FormattedValidationResult[0]).Select(
                                   result => result.Path == null ? result.Message.GetText(language) : "(" + result.Path.GetText(language) + ") " + result.Message.GetText(language)) /*.OrderBy(s => s)*/));
        }
    }
}