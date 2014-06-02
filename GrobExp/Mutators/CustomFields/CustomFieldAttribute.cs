using System;

namespace GrobExp.Mutators.CustomFields
{
    public class CustomFieldAttribute : Attribute
    {
        public CustomFieldAttribute(Type titleType = null)
        {
            TitleType = titleType;
        }

        public Type TitleType { get; private set; }
    }
}