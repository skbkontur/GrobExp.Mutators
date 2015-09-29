using System;

namespace GrobExp.Mutators
{
    public class CustomFieldsContainerAttribute: Attribute
    {
        
    }

    public interface IStringConverter
    {
        bool CanConvert(Type type);
        object Convert(string value, Type type);
        string Convert(object value, Type type);
    }
}