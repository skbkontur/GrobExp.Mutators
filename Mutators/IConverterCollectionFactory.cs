namespace GrobExp.Mutators
{
    public interface IConverterCollectionFactory
    {
        IConverterCollection<TSource, TDest> Get<TSource, TDest>();
        INewConverterCollection<TSource, TDest, TContext> Get<TSource, TDest, TContext>();
    }
}