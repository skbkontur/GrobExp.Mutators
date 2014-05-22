using System;

using GrobExp.Compiler;

namespace GrobExp.Mutators.CustomFields
{
    public class CustomFieldValue
    {
        public CustomFieldValue()
        {
        }

        public CustomFieldValue(ICustomFieldsConverter converter, TypeCode typeCode)
        {
            Converter = converter;
            this.typeCode = typeCode;
        }

        public void SetValue(string value)
        {
            StringValue = value;
            this.value = Converter.ConvertFromString(value, typeCode);
        }

        public object Value
        {
            get { return value; }
            set
            {
                if(value != null)
                {
                    var curTypeCode = GetTypeCode(value.GetType());
                    if(typeCode == System.TypeCode.Empty)
                        typeCode = curTypeCode;
                    else if(curTypeCode != typeCode)
                        throw new InvalidOperationException("Cannot change typecode");
                }
                this.value = value;
                StringValue = value == null ? null : Converter.ConvertToString(value);
            }
        }

        public string StringValue { get; private set; }

        public int TypeCode { get { return (int)typeCode; } }

        public ICustomFieldsConverter Converter { get; set; }

        private static TypeCode GetTypeCode(Type type)
        {
            return type.IsNullable() ? GetTypeCode(type.GetGenericArguments()[0]) : Type.GetTypeCode(type);
        }

        private object value;
        private TypeCode typeCode;
    }
}