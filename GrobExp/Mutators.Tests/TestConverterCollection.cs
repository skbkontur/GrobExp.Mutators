using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GrobExp.Mutators;

namespace Mutators.Tests
{
    public class TestStringConverter : IStringConverter
    {
        private static readonly Dictionary<string, TestCustomFields.TestEnum> beautifulNameToEnum;
        private static readonly Dictionary<TestCustomFields.TestEnum, string> enumToBeautifulName;

        static TestStringConverter()
        {
            var fields = typeof(TestCustomFields.TestEnum).GetFields(BindingFlags.Static | BindingFlags.Public);
            var enumValues = fields.Select(field => (TestCustomFields.TestEnum)Enum.Parse(typeof(TestCustomFields.TestEnum), field.Name)).ToArray();
            beautifulNameToEnum = new Dictionary<string, TestCustomFields.TestEnum>();
            enumToBeautifulName = new Dictionary<TestCustomFields.TestEnum, string>();
            for(int i = 0; i < fields.Length; ++i)
            {
                var name = ((TestCustomFields.BeatifulNameAttribute)fields[i].GetCustomAttribute(typeof(TestCustomFields.BeatifulNameAttribute))).Name;
                beautifulNameToEnum.Add(name, enumValues[i]);
                enumToBeautifulName.Add(enumValues[i], name);
            }
        }

        public bool CanConvert(Type type)
        {
            return type == typeof(TestCustomFields.TestEnum);
        }

        public object Convert(string value, Type type)
        {
            if(type != typeof(TestCustomFields.TestEnum))
                return null;
            TestCustomFields.TestEnum result;
            return value != null && beautifulNameToEnum.TryGetValue(value, out result) ? result : (object)null;
        }

        public string Convert(object value, Type type)
        {
            if (type != typeof(TestCustomFields.TestEnum))
                return null;
            string result;
            return enumToBeautifulName.TryGetValue((TestCustomFields.TestEnum)value, out result) ? result : null;
        }
    }

    public class TestConverterCollection<TSource, TDest> : ConverterCollection<TSource, TDest> where TDest : new()
    {
        public TestConverterCollection(IPathFormatterCollection pathFormatterCollection, Action<ConverterConfigurator<TSource, TDest>> action)
            : base(pathFormatterCollection, new TestStringConverter())
        {
            this.action = action;
        }

        protected override void Configure(MutatorsContext context, ConverterConfigurator<TSource, TDest> configurator)
        {
            action(configurator);
        }

        private readonly Action<ConverterConfigurator<TSource, TDest>> action;
    }
}