using System;

namespace GrobExp.Mutators.MultiLanguages
{
    public class UnknownLanguageException : Exception
    {
        public UnknownLanguageException(string language)
            : base(language)
        {
        }
    }
}