namespace GrobExp.Mutators.MultiLanguages
{
    public abstract class StaticMultiLanguageTextBase : MultiLanguageTextBase
    {
        protected void Register(string language, string text)
        {
            Register(language, () => text);
        }

        protected void Register(string language, string context, string text)
        {
            Register(language, context, () => text);
        }
    }
}