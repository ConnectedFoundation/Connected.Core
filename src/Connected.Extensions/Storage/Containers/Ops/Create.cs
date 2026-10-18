using Connected.Services;

namespace Connected.Storage.Containers.Ops;

/// <summary>
/// Creates a storage container through the container provider of the active storage connection type.
/// </summary>
internal sealed class Create(IMiddlewareService middleware)
	: ServiceFunction<IStorageContainerDto, string>
{
	/// <inheritdoc/>
	protected override async Task<string> OnInvoke() => await (await middleware.ResolveContainerProvider()).Create(Dto);
}
