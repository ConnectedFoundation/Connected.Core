using Connected.Services;

namespace Connected.Storage.Containers.Ops;

/// <summary>
/// Drops a storage container through the container provider of the active storage connection type.
/// </summary>
internal sealed class Drop(IMiddlewareService middleware)
	: ServiceAction<IStorageContainerDto>
{
	/// <inheritdoc/>
	protected override async Task OnInvoke() => await (await middleware.ResolveContainerProvider()).Drop(Dto);
}
