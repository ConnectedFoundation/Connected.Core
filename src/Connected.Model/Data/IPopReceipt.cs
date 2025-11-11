namespace Connected.Data;
/// <summary>
/// Represents an entity with a conditional visibility lock enforced through a pop receipt.
/// </summary>
/// <remarks>
/// Some entities require singleton access preventing concurrent processing. A queue message is one example: it must be processed
/// by only one client within a limited time window. The <c>PopReceipt</c> is updated each time a client dequeues the entity, granting
/// exclusive access until <see cref="NextVisible"/> is reached. Once expired, other clients may attempt processing. Clients that do not
/// complete work in time relinquish the lock automatically as visibility returns.
/// </remarks>
public interface IPopReceipt
{
	/// <summary>
	/// Gets the pop receipt identifier granted to the current processing scope until expiration.
	/// </summary>
	/// <remarks>
	/// The pop receipt is null when no exclusive lock is currently held (e.g., prior to initial dequeue or after expiration).
	/// </remarks>
	Guid? PopReceipt { get; init; }
	/// <summary>
	/// Gets the date and time at which the current pop receipt expires and visibility returns to other clients.
	/// </summary>
	/// <remarks>
	/// Upon reaching this timestamp, the entity becomes eligible for dequeue by other clients, enabling retry or reassignment.
	/// </remarks>
	DateTimeOffset NextVisible { get; init; }
}
