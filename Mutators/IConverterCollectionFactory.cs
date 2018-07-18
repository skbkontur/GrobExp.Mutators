namespace GrobExp.Mutators
{
    public interface IConverterCollectionFactory
    {
        IConverterCollection<TSource, TDest> Get<TSource, TDest>();
    }
}