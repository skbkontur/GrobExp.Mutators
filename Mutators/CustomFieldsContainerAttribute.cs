using System;

namespace GrobExp.Mutators
{
    public class CustomFieldsContainerAttribute : Attribute
    {
    }

    public interface IStringConverter
    {
        bool CanConvert(Type type);
        object ConvertFromString<T>(string value);
        string ConvertToString<T>(object value);
        string ConvertToString(object value, Type type);
        object ConvertFromString(string value, Type type);
    }

    public abstract class StringConverterBase : IStringConverter
    {
        public abstract bool CanConvert(Type type);
        public abstract string ConvertToString(object value, Type type);
        public abstract object ConvertFromString(string value, Type type);

        public object ConvertFromString<T>(string value)
        {
            return ConvertFromString(value, typeof(T));
        }

        public string ConvertToString<T>(object value)
        {
            return ConvertToString(value, typeof(T));
        }
    }
}