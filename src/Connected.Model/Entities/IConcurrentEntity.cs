using Connected.Annotations.Entities;

namespace Connected.Entities;
/// <summary>
/// Provides in-memory concurrency control for frequently updated cached entities using a synchronization value.
/// </summary>
/// <remarks>
/// Ensures application-level consistency by rejecting writes where the <see cref="Sync"/> value is stale. Entities are immutable; cache replaces
/// whole instances. Before replacing, implementations compare synchronization values to prevent overwriting newer data. While <see cref="IConsistentEntity{T}"/>
/// focuses on storage-level consistency (e.g., optimistic concurrency), this interface addresses concurrent in-memory updates. It is not inherently thread-safe;
/// external coordination (e.g., atomic cache operations) must enforce synchronization.
/// </remarks>
/// <typeparam name="TPrimaryKey">Primary key type.</typeparam>
public interface IConcurrentEntity<TPrimaryKey> : IConsistentEntity<TPrimaryKey>
	 where TPrimaryKey : notnull
{
	/// <summary>
	/// Gets or sets the synchronization value used to detect stale writes.
	/// </summary>
	/// <remarks>
	/// Incremented when a newer version of the entity is written to cache. If an update attempts to replace an entity whose synchronization value no longer matches,
	/// the write should be rejected to avoid losing intervening changes.
	/// </remarks>
	/// <example>
	/// Multiple threads dequeue queue messages and overwrite cached entities. Each successful write increments <see cref="Sync"/>. Subsequent writes with earlier
	/// synchronization values fail, preventing lost updates.
	/// </example>
	[Persistence(PersistenceMode.InMemory)]
	int Sync { get; set; }
}