using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators.Validators.Texts
{
    [MultiLanguageTextType("LengthOutOfRangeText")]
    public class LengthOutOfRangeText : MultiLanguageTextBaseWithPath
    {
        public int? From { get; set; }
        public int? To { get; set; }

        public MultiLanguageTextBase Title { get; set; }

        protected override void Register()
        {
            Register("RU", () => "Значение"
                                 + (Title == null ? "" : " «" + Title.GetText("RU") + "»")
                                 + (Value == null ? "" : (" '" + Value + "'"))
                                 + (Path == null ? "" : " (" + Path.GetText("RU") + ")")
                                 + " должно содержать "
                                 + (From == null ? ("не больше " + To) : ("от " + From + " до " + To)) + " символов");
            //Register("EN", () => "The value " + (Title == null ? "" : ("«" + Title.GetText("EN") + "»")) + (Value == null ? "" : ("('" + Value + "')")) + " must contain " + (From == null ? ("no more than " + To) : (From + " from " + To)) + " characters");
            Register("RU", Web, () => "Значение должно содержать " + (From == null ? ("не больше " + To) : ("от " + From + " до " + To)) + " символов");
            //Register("EN", Web, () => "The value must contain " + (From == null ? ("no more than " + To) : (From + " from " + To)) + " characters");
        }
    }
}