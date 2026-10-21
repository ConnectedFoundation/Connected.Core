using Connected.Annotations;

namespace Connected.Storage.Containers;

/// <summary>
/// Creates and drops storage containers beside the default storage.
/// </summary>
/// <remarks>
/// A storage container is what a storage provider keeps a separate set of entities in: a database for a relational
/// provider, a folder for a file store, a prefix for a table store. Containers are meant for components that keep
/// data apart from the default storage, such as disposable test environments; schema synchronization creates the
/// entities' structures in a container when an application starts on its connection string. The work is done by the
/// <see cref="IStorageContainerProvider"/> serving the storage connection type in use, so callers stay independent of
/// the storage product. None of the operations is exposed over REST.
/// </remarks>
[Service]
public interface IStorageContainerService
{
	/// <summary>
	/// Creates a container beside the default storage, unless it exists.
	/// </summary>
	/// <param name="dto">The DTO naming the container.</param>
	/// <returns>The connection string an application stores its entities in the container with.</returns>
	/// <exception cref="InvalidOperationException">Storage that is not a container exists under the name.</exception>
	/// <exception cref="NotSupportedException">No container provider serves the active storage connection type.</exception>
	Task<string> Create(IStorageContainerDto dto);

	/// <summary>
	/// Drops a container beside the default storage, with everything in it, if it exists.
	/// </summary>
	/// <param name="dto">The DTO naming the container.</param>
	/// <exception cref="NotSupportedException">No container provider serves the active storage connection type.</exception>
	/// <exception cref="InvalidOperationException">The container is the default storage, or storage that is not a container exists under the name.</exception>
	Task Drop(IStorageContainerDto dto);
}
