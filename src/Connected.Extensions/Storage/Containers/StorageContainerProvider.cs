using Connected.Configuration;

namespace Connected.Storage.Containers;

/// <summary>
/// The base class of storage container providers.
/// </summary>
/// <param name="configuration">The configuration holding the default connection string.</param>
/// <remarks>
/// Gives derived providers the default connection string their containers sit beside, and refuses to drop the default
/// storage before a derived provider sees the request. Every container a provider creates carries
/// <see cref="ContainerMarker"/>, and a provider neither adopts nor drops anything that does not, so a container can
/// never be confused with storage another application owns.
/// </remarks>
public abstract class StorageContainerProvider(IConfigurationService configuration)
	: Middleware, IStorageContainerProvider
{
	/// <inheritdoc/>
	public abstract Type ConnectionType { get; }

	/// <summary>
	/// The mark every container carries.
	/// </summary>
	protected const string ContainerMarker = "connected:storage-container";

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
		var defaultName = await DefaultName();

		if (string.IsNullOrEmpty(defaultName))
			throw new InvalidOperationException("The default storage's name cannot be determined, so no container is dropped.");

		if (string.Equals(dto.Name, defaultName, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("The default storage cannot be dropped.");

		await OnDrop(dto.Name);
	}

	/// <summary>
	/// Returns the name of the container the default connection string stores in, as the server resolves it, so a
	/// connection string that names none still yields the container actually used.
	/// </summary>
	/// <returns>The default container's name.</returns>
	protected abstract Task<string?> DefaultName();

	/// <summary>
	/// Returns the exception thrown for storage that exists under a container's name but is not a container.
	/// </summary>
	/// <param name="name">The container's name.</param>
	/// <returns>The exception.</returns>
	protected static InvalidOperationException NotAContainer(string name) => new($"The storage '{name}' exists but is not a storage container.");

	/// <summary>
	/// Creates a container beside the default storage and marks it, unless the container exists.
	/// </summary>
	/// <exception cref="InvalidOperationException">Storage that is not a container exists under the name.</exception>
	/// <param name="name">The container's name, validated as letters, digits and underscores.</param>
	/// <returns>The connection string an application stores its entities in the container with.</returns>
	protected abstract Task<string> OnCreate(string name);

	/// <summary>
	/// Drops a container beside the default storage, if it exists; it is never the default storage.
	/// </summary>
	/// <exception cref="InvalidOperationException">Storage that is not a container exists under the name.</exception>
	/// <param name="name">The container's name, validated as letters, digits and underscores.</param>
	protected abstract Task OnDrop(string name);
}
