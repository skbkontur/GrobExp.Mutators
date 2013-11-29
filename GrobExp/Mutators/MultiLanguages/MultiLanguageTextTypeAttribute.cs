using System;

namespace GrobExp.Mutators.MultiLanguages
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MultiLanguageTextTypeAttribute : Attribute
    {
        public MultiLanguageTextTypeAttribute(string type)
        {
            Type = type;
        }

        public string Type { get; private set; }
    }
}