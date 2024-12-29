using Microsoft.Extensions.Configuration;

namespace Connected.Configuration;

internal class StorageConfiguration(IConfiguration configuration) : IStorageConfiguration
{
	public IDatabaseConfiguration Databases { get; } = new DatabaseConfiguration(configuration.GetSection("databases"));
}
