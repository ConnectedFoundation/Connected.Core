using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Collections.Queues.Serialization;
using Connected.Entities;
using Connected.Services;
using System.Data;

namespace Connected.Collections.Queues;
/// <summary>
/// Provides an abstract base class for queue message entities with serialization metadata and concurrent entity support.
/// </summary>
/// <remarks>
/// QueueMessage implements the IQueueMessage interface as a record type with concurrent entity tracking (row version).
/// It defines the standard property set for queue messages including:
/// - DTO payload with binary serialization using <see cref="DtoSerializer"/>
/// - Action type with string serialization using <see cref="TypeSerializer"/>
/// - Metadata including creation timestamp, priority, expiration, and visibility window
/// - Dequeue tracking including dequeue count, timestamp, and pop receipt
/// - Group identifier for debouncing and duplicate prevention
/// Properties are decorated with ordinal positions, data types, and custom serializers for ORM mapping.
/// Derived classes must provide concrete implementations for specific queue storage schemas.
/// </remarks>
public abstract record QueueMessage
	: ConcurrentEntity<long>, IQueueMessage
{
	/// <summary>
	/// Gets the timestamp when the message was created and enqueued.
	/// </summary>
	[Ordinal(0), Date(DateKind.DateTime)]
	public DateTimeOffset Created { get; init; }

	/// <summary>
	/// Gets the number of times the message has been dequeued for processing.
	/// </summary>
	/// <remarks>
	/// This count increments each time the message is dequeued by a queue host.
	/// When it reaches MaxDequeueCount, the message is deleted as poison.
	/// </remarks>
	[Ordinal(1)]
	public int DequeueCount { get; init; }

	/// <summary>
	/// Gets the timestamp when the message was last dequeued.
	/// </summary>
	[Ordinal(2), Date(DateKind.DateTime2, 7)]
	public DateTimeOffset? DequeueTimestamp { get; init; }

	/// <summary>
	/// Gets the fully qualified type name of the DTO for deserialization.
	/// </summary>
	/// <remarks>
	/// This property stores the assembly-qualified type name used to deserialize the binary Dto property.
	/// Format: "Namespace.TypeName, AssemblyName"
	/// </remarks>
	[Ordinal(3), Length(1024)]
	public string DtoTypeName { get; init; } = default!;

	/// <summary>
	/// Gets the data transfer object containing the message payload.
	/// </summary>
	/// <remarks>
	/// The DTO is serialized to binary format using JSON and stored in a VARBINARY column.
	/// Deserialization uses the DtoTypeName property to resolve the concrete type.
	/// </remarks>
	[Ordinal(4), Length(1024), DataType(DbType.Binary)]
	[Serializer(typeof(DtoSerializer))]
	public IDto Dto { get; init; } = default!;

	/// <summary>
	/// Gets the action type that will process this message.
	/// </summary>
	/// <remarks>
	/// The action type is stored as an assembly-qualified type name string and deserialized at runtime
	/// to resolve the IQueueAction implementation from the dependency injection container.
	/// </remarks>
	[Ordinal(5), Length(1024), DataType(DbType.String)]
	[Serializer(typeof(TypeSerializer))]
	public Type Action { get; init; } = default!;

	/// <summary>
	/// Gets the timestamp when the message becomes visible for dequeuing.
	/// </summary>
	/// <remarks>
	/// Messages are not visible to queue hosts until this timestamp is reached.
	/// This value is extended during processing to prevent duplicate dequeuing.
	/// </remarks>
	[Ordinal(6), Date(DateKind.DateTime2, 7)]
	public DateTimeOffset NextVisible { get; init; }

	/// <summary>
	/// Gets the unique pop receipt assigned when the message was dequeued.
	/// </summary>
	/// <remarks>
	/// Pop receipts are generated during dequeue operations and used to track in-flight messages.
	/// They enable visibility extensions and message completion/deletion after processing.
	/// </remarks>
	[Ordinal(7)]
	public Guid? PopReceipt { get; init; }

	/// <summary>
	/// Gets the group identifier used for debouncing and duplicate prevention.
	/// </summary>
	/// <remarks>
	/// Messages with the same action type and group are subject to debouncing logic.
	/// Typically derived from entity identifiers to prevent duplicate processing.
	/// </remarks>
	[Ordinal(8), Length(256)]
	public string? Group { get; init; }

	/// <summary>
	/// Gets the expiration timestamp after which the message is automatically deleted.
	/// </summary>
	[Ordinal(9), Date(DateKind.DateTime)]
	public DateTimeOffset Expire { get; init; } = default!;

	/// <summary>
	/// Gets the message priority for ordering during dequeue operations.
	/// </summary>
	/// <remarks>
	/// Higher priority messages are dequeued before lower priority messages.
	/// Within the same priority, messages are ordered by NextVisible and then by Id.
	/// </remarks>
	[Ordinal(10)]
	public int Priority { get; init; } = default!;

	/// <summary>
	/// Gets the maximum number of dequeue attempts before the message is deleted as poison.
	/// </summary>
	/// <remarks>
	/// Once this threshold is reached, the message is removed during target selection to prevent
	/// indefinite retry of failing work.
	/// </remarks>
	[Ordinal(12)]
	public int MaxDequeueCount { get; init; }

	/// <summary>
	/// Gets the visibility extension interval, in seconds, used while processing is active.
	/// </summary>
	/// <remarks>
	/// Queue host and queue job components use this value to extend <see cref="NextVisible"/> during
	/// dequeue reservation and ping updates.
	/// </remarks>
	[Ordinal(13)]
	public int PopInterval { get; init; }
}
