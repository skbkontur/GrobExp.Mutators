using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators.Validators.Texts
{
    [MultiLanguageTextType("ValueRequiredText")]
    public class ValueRequiredText : MultiLanguageTextBaseWithPath
    {
        public MultiLanguageTextBase Title { get; set; }

        protected override void Register()
        {
            Register("RU", () => "Значение"
                                 + (Title == null ? "" : " «" + Title.GetText("RU") + "»")
                                 + (Path == null ? "" : " (" + Path.GetText("RU") + ")")
                                 + " обязательно");
            Register("EN", () => "Value"
                                 + (Title == null ? "" : " «" + Title.GetText("EN") + "»")
                                 + (Path == null ? "" : " (" + Path.GetText("EN") + ")")
                                 + " is required");
            Register("RU", Web, () => "Поле должно быть заполнено");
            Register("EN", Web, () => "A value is required");
        }
    }
}