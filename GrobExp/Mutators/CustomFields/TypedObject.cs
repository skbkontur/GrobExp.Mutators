using System;

namespace GrobExp.Mutators.CustomFields
{
    public class TypedObject
    {
        public object Value { get; set; }
        public TypeCode TypeCode { get; set; }
    }
}