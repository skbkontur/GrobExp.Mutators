using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators.Validators.Texts
{
    [MultiLanguageTextType("ValueShouldMatchPatternText")]
    public class ValueShouldMatchPatternText : MultiLanguageTextBaseWithPath
    {
        public MultiLanguageTextBase Title { get; set; }
        public string Pattern { get; set; }

        protected override void Register()
        {
            Register("RU", () => "Значение"
                                 + (Title == null ? "" : " «" + Title.GetText("RU") + "»")
                                 + (Path == null ? "" : " (" + Path.GetText("RU") + ")")
                                 + " должно удовлетворять регулярному выражению '" + Pattern + "'");
            Register("EN", () => "Value"
                                 + (Title == null ? "" : " «" + Title.GetText("EN") + "»")
                                 + (Path == null ? "" : " (" + Path.GetText("EN") + ")")
                                 + " should match regular expression '" + Pattern + "'");
            Register("RU", Web, () => "Значение должно удовлетворять регулярному выражению '" + Pattern + "'");
            Register("EN", Web, () => "A value should match regular expression '" + Pattern + "'");
        }
    }
}