using Connected.Data.Expressions.Evaluation;
using Connected.Services;

namespace Connected.Storage.Containers;

/// <summary>
/// Resolves the container provider of the active storage connection type.
/// </summary>
internal static class ContainerProviderExtensions
{
	/// <summary>
	/// Returns the container provider serving the connection type the storage resolves.
	/// </summary>
	/// <param name="middleware">The middleware service.</param>
	/// <returns>The container provider.</returns>
	/// <exception cref="NotSupportedException">No storage connection provider proposes a connection, or no container provider serves its type.</exception>
	/// <remarks>
	/// The connection type is chosen by <c>ConnectionProvider</c>, with the rule it applies to every storage operation.
	/// </remarks>
	public static async Task<IStorageContainerProvider> ResolveContainerProvider(this IMiddlewareService middleware)
	{
		var context = DtoFactory.Create<IStorageContextDto>(f =>
		{
			f.ConnectionMode = StorageConnectionMode.Shared;
			f.Operation = new StorageOperation { Concurrency = DataConcurrencyMode.Disabled };
		});

		var connectionType = await ConnectionProvider.ResolveConnectionType<ContainerEntity>(middleware, context)
			?? throw new NotSupportedException("No storage connection provider is registered.");

		return (await middleware.Query<IStorageContainerProvider>()).FirstOrDefault(f => f.ConnectionType == connectionType)
			?? throw new NotSupportedException($"No storage container provider serves the storage connection type '{connectionType.Name}'.");
	}
}
