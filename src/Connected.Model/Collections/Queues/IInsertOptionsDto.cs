using Connected.Services;

namespace Connected.Collections.Queues;

/// <summary>
/// DTO specifying options used when inserting a message into a queue.
/// </summary>
public interface IInsertOptionsDto
	: IDto
{
	/// <summary>
	/// Gets or sets the expiration date after which the message is automatically deleted.
	/// </summary>
	DateTimeOffset Expire { get; set; }

	/// <summary>
	/// Gets or sets an optional batch identifier used to group or deduplicate messages.
	/// </summary>
	string? Batch { get; set; }

	/// <summary>
	/// Gets or sets the message priority; higher values are processed first.
	/// </summary>
	int Priority { get; set; }

	/// <summary>
	/// Gets or sets the next visibility time after which the message becomes visible again if not processed.
	/// </summary>
	DateTimeOffset? NextVisible { get; set; }

	/// <summary>
	/// Gets or sets the target queue name.
	/// </summary>
	string Queue { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of dequeue attempts before the message is deleted.
	/// </summary>
	int MaxDequeueCount { get; set; }
}

