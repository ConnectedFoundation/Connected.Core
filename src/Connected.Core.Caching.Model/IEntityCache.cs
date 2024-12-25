namespace Connected.Caching;

public interface IEntityCache<TEntry, TKey> : ISynchronizedCache<TEntry, TKey>
{
	Task Refresh(TKey id);
	Task Remove(TKey id);
}
