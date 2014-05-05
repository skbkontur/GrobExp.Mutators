using System;

namespace GrobExp.Mutators.CustomFields
{
    public class CustomFieldValue
    {
        private readonly ICustomFieldsConverter converter;

        public CustomFieldValue(ICustomFieldsConverter converter)
        {
            this.converter = converter;
        }

        private object value;
        private TypeCode typeCode;

        public object Value
        {
            get { return value; }
            set
            {
                this.value = value;
                typeCode = value == null ? TypeCode.Empty : Type.GetTypeCode(value.GetType());
                StringValue = value == null ? null : converter.ConvertToString(value);
            }
        }

        public void SetValue(string value, TypeCode typeCode)
        {
            StringValue = value;
            this.typeCode = typeCode;
            this.value = converter.ConvertFromString(value, typeCode);
        }

        public string StringValue { get; private set; }

        public static CustomFieldValue operator +(CustomFieldValue left, CustomFieldValue right)
        {
            throw new NotImplementedException();
        }
    }
}