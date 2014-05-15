using System;

using GrobExp.Compiler;

namespace GrobExp.Mutators.CustomFields
{
    public class CustomFieldValue
    {
        public CustomFieldValue(ICustomFieldsConverter converter, TypeCode typeCode)
        {
            this.converter = converter;
            this.typeCode = typeCode;
        }

        public void SetValue(string value)
        {
            StringValue = value;
            this.value = converter.ConvertFromString(value, typeCode);
        }

        public object Value
        {
            get { return value; }
            set
            {
                if(value != null)
                {
                    var curTypeCode = GetTypeCode(value.GetType());
                    if(curTypeCode != typeCode)
                        throw new InvalidOperationException();
                }
                this.value = value;
                StringValue = value == null ? null : converter.ConvertToString(value);
            }
        }

        public string StringValue { get; private set; }
        public int TypeCode { get { return (int)typeCode; } }

        private static TypeCode GetTypeCode(Type type)
        {
            return type.IsNullable() ? GetTypeCode(type.GetGenericArguments()[0]) : Type.GetTypeCode(type);
        }

        private readonly ICustomFieldsConverter converter;
        private object value;
        private readonly TypeCode typeCode;
    }
}