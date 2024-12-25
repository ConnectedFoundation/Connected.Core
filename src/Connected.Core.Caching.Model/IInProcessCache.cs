namespace Connected.Caching;

public interface IInProcessCache : ICache
{
	void Merge(ICache cache);
}
