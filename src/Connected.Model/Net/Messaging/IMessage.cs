using Connected.Services;

namespace Connected.Net.Messaging;

/// <summary>
/// Represents a message in the messaging queue system.
/// </summary>
/// <remarks>
/// This interface encapsulates all information associated with a queued message including
/// the target client, message identifiers, payload, visibility control, and expiration time.
/// Messages are the fundamental unit of communication in the messaging system, carrying data
/// from senders to recipients with delivery tracking and retry capabilities. The visibility
/// and expiration properties enable reliable delivery patterns where messages can be
/// temporarily hidden after delivery to implement retry logic with visibility timeouts.
/// This supports at-least-once delivery semantics with configurable retry behavior.
/// </remarks>
public interface IMessage
{
	/// <summary>
	/// Gets the client that should receive this message.
	/// </summary>
	/// <value>
	/// An <see cref="IClient"/> instance representing the target recipient.
	/// </value>
	/// <remarks>
	/// The client property identifies which client connection should receive this message,
	/// enabling the messaging system to route messages to their intended recipients.
	/// </remarks>
	IClient Client { get; }

	/// <summary>
	/// Gets the unique numeric identifier for this message.
	/// </summary>
	/// <value>
	/// An unsigned 64-bit integer uniquely identifying the message.
	/// </value>
	/// <remarks>
	/// The message identifier is used for acknowledgement tracking and message correlation.
	/// Clients use this identifier when acknowledging receipt of messages.
	/// </remarks>
	ulong Id { get; }

	/// <summary>
	/// Gets the optional string key for this message.
	/// </summary>
	/// <value>
	/// A string serving as an alternative message identifier, or null if not assigned.
	/// </value>
	/// <remarks>
	/// The key provides an alternative way to identify messages using string-based identifiers
	/// rather than numeric IDs. This is useful when messages need to be correlated with
	/// application-specific keys or when implementing deduplication logic.
	/// </remarks>
	string? Key { get; }

	/// <summary>
	/// Gets the message payload.
	/// </summary>
	/// <value>
	/// An <see cref="IDto"/> containing the message data.
	/// </value>
	/// <remarks>
	/// The DTO contains the actual data being transmitted in the message. This can be any
	/// data transfer object representing events, commands, notifications, or other application
	/// data that needs to be communicated between server and clients.
	/// </remarks>
	IDto Dto { get; }

	/// <summary>
	/// Gets or sets the time when this message becomes visible for delivery.
	/// </summary>
	/// <value>
	/// A <see cref="DateTime"/> indicating when the message can be dequeued for delivery.
	/// </value>
	/// <remarks>
	/// The next visible time controls when the message becomes eligible for delivery. After
	/// a message is dequeued but not acknowledged, its visibility time can be updated to
	/// make it invisible temporarily, implementing retry logic with exponential backoff or
	/// other retry strategies. Messages only appear in dequeue operations when the current
	/// time exceeds the next visible time.
	/// </remarks>
	DateTime NextVisible { get; set; }

	/// <summary>
	/// Gets the expiration time for this message.
	/// </summary>
	/// <value>
	/// A <see cref="DateTime"/> indicating when the message should be permanently removed.
	/// </value>
	/// <remarks>
	/// The expiration time determines how long the message should be retained in the queue.
	/// Messages that exceed their expiration time are removed during cleanup operations,
	/// preventing indefinite message accumulation and managing resource usage. This is
	/// particularly important for time-sensitive messages that lose value after a certain period.
	/// </remarks>
	DateTime Expire { get; }
}
