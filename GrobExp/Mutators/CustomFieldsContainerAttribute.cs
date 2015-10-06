using System;

namespace GrobExp.Mutators
{
    public class CustomFieldsContainerAttribute: Attribute
    {
        
    }

    public interface IStringConverter
    {
        bool CanConvert(Type type);
        object Convert<T>(string value);
        string Convert<T>(object value);
        string Convert(object value, Type type);
    }
}