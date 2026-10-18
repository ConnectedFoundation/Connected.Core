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
	/// The connection type is chosen with the rule <c>ConnectionProvider</c> applies to every storage operation: the
	/// first storage connection provider, in priority order, that proposes a connection decides it. The proposed
	/// connection is only inspected for its type and is never opened.
	/// </remarks>
	public static async Task<IStorageContainerProvider> ResolveContainerProvider(this IMiddlewareService middleware)
	{
		var context = DtoFactory.Create<IStorageContextDto>(f => f.ConnectionMode = StorageConnectionMode.Shared);
		Type? connectionType = null;

		foreach (var provider in await middleware.Query<IStorageConnectionProvider>())
		{
			var proposed = await provider.Invoke<ContainerEntity>(context);

			if (proposed.Count == 0)
				continue;

			connectionType = proposed[0].GetType();

			foreach (var connection in proposed)
				await connection.DisposeAsync();

			break;
		}

		if (connectionType is null)
			throw new NotSupportedException("No storage connection provider is registered.");

		return (await middleware.Query<IStorageContainerProvider>()).FirstOrDefault(f => f.ConnectionType == connectionType)
			?? throw new NotSupportedException($"No storage container provider serves the storage connection type '{connectionType.Name}'.");
	}
}
