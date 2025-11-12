namespace Connected.Configuration;
/// <summary>
/// Configuration root for storage-related settings.
/// </summary>
/// <remarks>
/// Provides database configuration grouping connection strings and shard mappings.
/// </remarks>
public interface IStorageConfiguration
{
	/// <summary>
	/// Gets database configuration settings.
	/// </summary>
	IDatabaseConfiguration Databases { get; }
}
