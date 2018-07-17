using System;

namespace GrobExp.Mutators
{
    public interface IConverterCollection<TSource, TDest>
    {
        /// <returns>
        ///     Функ, создающий новый экземпляр <typeparamref name="TDest" /> и конвертирующий в него данные из <typeparamref name="TSource" />
        /// </returns>
        Func<TSource, TDest> GetConverter(MutatorsContext context);

        /// <returns>
        ///     Экшен, записывающий данные из <typeparamref name="TSource" /> в существующий экземпляр <typeparamref name="TDest" />
        /// </returns>
        Action<TSource, TDest> GetMerger(MutatorsContext context);

        MutatorsTreeBase<TSource> Migrate(MutatorsTreeBase<TDest> mutatorsTree, MutatorsContext context);
        MutatorsTreeBase<TSource> GetValidationsTree(MutatorsContext context, int priority);
        MutatorsTreeBase<TDest> MigratePaths(MutatorsTreeBase<TDest> mutatorsTree, MutatorsContext context);
    }
}