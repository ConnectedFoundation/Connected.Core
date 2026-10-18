using Connected.Services;
using Connected.Storage.Containers.Ops;

namespace Connected.Storage.Containers;

/// <summary>
/// Implements <see cref="IStorageContainerService"/> by delegating to the container provider of the active storage connection type.
/// </summary>
internal sealed class StorageContainerService(IServiceProvider services)
	: Service(services), IStorageContainerService
{
	/// <inheritdoc/>
	public async Task<string> Create(IStorageContainerDto dto) => await Invoke(GetOperation<Create>(), dto);

	/// <inheritdoc/>
	public async Task Drop(IStorageContainerDto dto) => await Invoke(GetOperation<Drop>(), dto);
}
