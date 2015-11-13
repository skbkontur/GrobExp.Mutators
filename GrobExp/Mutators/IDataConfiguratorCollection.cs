using System;

namespace GrobExp.Mutators
{
    public interface IDataConfiguratorCollection<TData>
    {
        MutatorsTree<TData> GetMutatorsTree(MutatorsContext context, int validationPriority = 0);
        MutatorsTree<TData> GetMutatorsTree(Type[] path, MutatorsContext[] mutatorsContexts, MutatorsContext[] converterContexts);
        void Clear();
    }

    public static class DataConfiguratorCollectionExtensions
    {
        public static MutatorsTree<TDest> GetMutatorsTree<TSource, TDest>(this IDataConfiguratorCollection<TDest> collection, MutatorsContext sourceMutatorsContext, MutatorsContext destMutatorsContext, MutatorsContext converterContext)
        {
            return collection.GetMutatorsTree(new[] {typeof(TSource)}, new[] {sourceMutatorsContext, destMutatorsContext}, new[] {converterContext});
        }
    }
}