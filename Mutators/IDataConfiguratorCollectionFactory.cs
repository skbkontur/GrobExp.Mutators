namespace GrobExp.Mutators
{
    public interface IDataConfiguratorCollectionFactory
    {
        IDataConfiguratorCollection<T> Get<T>();
    }
}