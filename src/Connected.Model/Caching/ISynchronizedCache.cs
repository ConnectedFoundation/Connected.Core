namespace Connected.Caching;

public interface ISynchronizedCache<TEntry, TKey> : ICachingDataProvider, ICacheContainer<TEntry, TKey>
{
}
