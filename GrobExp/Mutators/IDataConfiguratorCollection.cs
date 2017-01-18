using System;

namespace GrobExp.Mutators
{
    public interface IDataConfiguratorCollection<TData>
    {
        MutatorsTreeBase<TData> GetMutatorsTree(MutatorsContext context, int validationPriority = 0);
        MutatorsTreeBase<TData> GetMutatorsTree(Type[] path, MutatorsContext[] mutatorsContexts, MutatorsContext[] converterContexts);
    }

    public static class DataConfiguratorCollectionExtensions
    {
        public static MutatorsTreeBase<TDest> GetMutatorsTree<TSource, TDest>(this IDataConfiguratorCollection<TDest> collection, MutatorsContext sourceMutatorsContext, MutatorsContext destMutatorsContext, MutatorsContext converterContext)
        {
            return collection.GetMutatorsTree(new[] {typeof(TSource)}, new[] {sourceMutatorsContext, destMutatorsContext}, new[] {converterContext});
        }
    }
}