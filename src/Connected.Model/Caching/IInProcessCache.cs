namespace Connected.Caching;

public interface IInProcessCache : ICache
{
	void Merge(ICacheContext cache);
}
