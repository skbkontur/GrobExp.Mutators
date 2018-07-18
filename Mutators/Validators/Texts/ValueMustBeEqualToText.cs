using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators.Validators.Texts
{
    [MultiLanguageTextType("ValueMustBeEqualToText")]
    public class ValueMustBeEqualToText : MultiLanguageTextBaseWithPath
    {
        public MultiLanguageTextBase Title { get; set; }
        public object ActualValue { get; set; }
        public object ExpectedValue { get; set; }

        protected override void Register()
        {
            Register("RU", () => "Значение"
                                 + (Title == null ? "" : " «" + Title.GetText("RU") + "»")
                                 + (ActualValue == null ? "" : " '" + ActualValue + "'")
                                 + (Path == null ? "" : " (" + Path.GetText("RU") + ")")
                                 + " должно быть равно '"
                                 + ExpectedValue + "'");
            //Register("EN", () => "The field must be equal to " + Value);
        }
    }
}