using System;

using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators.CustomFields
{
    public class CustomFieldValue
    {
        public object Value { get; set; }
        public TypeCode TypeCode { get; set; }
        public StaticMultiLanguageTextBase Title { get; set; }
    }
}