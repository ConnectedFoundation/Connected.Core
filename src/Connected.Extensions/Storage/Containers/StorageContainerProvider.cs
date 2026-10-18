using Connected.Configuration;

namespace Connected.Storage.Containers;

/// <summary>
/// The base class of storage container providers.
/// </summary>
/// <param name="configuration">The configuration holding the default connection string.</param>
/// <remarks>
/// Gives derived providers the default connection string their containers sit beside, and refuses to drop the default
/// storage before a derived provider sees the request.
/// </remarks>
public abstract class StorageContainerProvider(IConfigurationService configuration)
	: Middleware, IStorageContainerProvider
{
	/// <inheritdoc/>
	public abstract Type ConnectionType { get; }

	/// <summary>
	/// Gets the default connection string.
	/// </summary>
	/// <exception cref="InvalidOperationException">No default connection string is configured.</exception>
	protected string DefaultConnectionString => configuration.Storage.Databases.DefaultConnectionString
		?? throw new InvalidOperationException("No default connection string is configured.");

	/// <inheritdoc/>
	public async Task<string> Create(IStorageContainerDto dto)
	{
		return await OnCreate(dto.Name);
	}

	/// <inheritdoc/>
	public async Task Drop(IStorageContainerDto dto)
	{
		if (string.Equals(dto.Name, DefaultName(), StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("The default storage cannot be dropped.");

		await OnDrop(dto.Name);
	}

	/// <summary>
	/// Returns the name of the container the default connection string stores in.
	/// </summary>
	/// <returns>The default container's name.</returns>
	protected abstract string DefaultName();

	/// <summary>
	/// Creates a container beside the default storage, unless it exists.
	/// </summary>
	/// <param name="name">The container's name, validated as letters, digits and underscores.</param>
	/// <returns>The connection string an application stores its entities in the container with.</returns>
	protected abstract Task<string> OnCreate(string name);

	/// <summary>
	/// Drops a container beside the default storage, if it exists; it is never the default storage.
	/// </summary>
	/// <param name="name">The container's name, validated as letters, digits and underscores.</param>
	protected abstract Task OnDrop(string name);
}
