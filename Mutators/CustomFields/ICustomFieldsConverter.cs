using System;

namespace GrobExp.Mutators.CustomFields
{
    public interface ICustomFieldsConverter
    {
        string ConvertToString(object value);
        object ConvertFromString(string value, TypeCode typeCode);
        Type GetType(TypeCode typeCode);
    }
}