using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators.Validators.Texts
{
    [MultiLanguageTextType("LengthNotExactlyEqualsText")]
    public class LengthNotExactlyEqualsText : MultiLanguageTextBaseWithPath
    {
        public int? Exactly { get; set; }
        public string Value { get; set; }

        public MultiLanguageTextBase Title { get; set; }

        protected override void Register()
        {
            Register("RU", () => "Значение"
                                 + (Title == null ? "" : " «" + Title.GetText("RU") + "»")
                                 + (Value == null ? "" : (" '" + Value + "'"))
                                 + (Path == null ? "" : " (" + Path.GetText("RU") + ")")
                                 + " должно содержать ровно " + Exactly + " символов");
            Register("RU", Web, () => "Значение должно содержать " + Exactly + " символов");
        }
    }
}