using System;

namespace GrobExp.Mutators
{
    public interface IConverterCollection<TSource, TDest>
    {
        Func<TSource, TDest> GetConverter(MutatorsContext context);
        Action<TSource, TDest> GetMerger(MutatorsContext context);

        MutatorsTree<TSource> Migrate(MutatorsTree<TDest> mutatorsTree, MutatorsContext context);
        MutatorsTree<TSource> GetValidationsTree(MutatorsContext context, int priority);
    }
}