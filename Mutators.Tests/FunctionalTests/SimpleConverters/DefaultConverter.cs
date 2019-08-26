namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class DefaultConverter
    {
        public T Convert<T>(T value) => value;

        public T ConvertWithDefault<T>(T value, T defaultValue) where T : class => value == defaultValue ? null : value;
    }
}