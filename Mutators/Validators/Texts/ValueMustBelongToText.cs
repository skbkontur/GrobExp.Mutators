using System.Linq;

using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators.Validators.Texts
{
    [MultiLanguageTextType("ValueMustBelongToText")]
    public class ValueMustBelongToText : MultiLanguageTextBaseWithPath
    {
        public StaticMultiLanguageTextBase Title { get; set; }
        public object[] Values { get; set; }

        protected override void Register()
        {
            Register("RU", () => "Значение"
                                 + (Title == null ? "" : (" «" + Title.GetText("RU") + "»"))
                                 + (Value == null ? "" : (" '" + Value + "'"))
                                 + (Path == null ? "" : " (" + Path.GetText("RU") + ")")
                                 + " не распознано"
                                 + (Values == null ? "" : ". Допустимые значения: " + string.Join(", ", Values.Select(o => "'" + o.ToString() + "'"))));
            //Register("EN", () => "The value " + (Title == null ? "" : ("«" + Title.GetText("EN") + "»")) + (Value == null ? "" : ("('" + Value + "')")) + " is not recognized." + (Values == null ? "" : " Admissible values: {" + string.Join(", ", Values.Select(o => "'" + o.ToString() + "'")) + "}"));
        }
    }
}