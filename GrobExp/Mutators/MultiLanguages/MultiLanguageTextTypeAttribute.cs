using System;

namespace GrobExp.Mutators.MultiLanguages
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MultiLanguageTextTypeAttribute : Attribute
    {
        public MultiLanguageTextTypeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}