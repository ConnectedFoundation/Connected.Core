namespace Connected.Caching;

public class CacheEntryOptions : IEntryOptions
{
	public string Key { get; set; }
	public string? KeyProperty { get; set; }
	public TimeSpan Duration { get; set; }
	public bool SlidingExpiration { get; set; }
	public bool AllowNull { get; set; }

	public CacheEntryOptions()
	{
		Duration = TimeSpan.FromMinutes(5);
		SlidingExpiration = true;
		Key = ".";
	}
}
