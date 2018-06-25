namespace GrobExp.Mutators
{
    public interface IPathFormatterCollection
    {
        IPathFormatter GetPathFormatter<T>();
    }
}