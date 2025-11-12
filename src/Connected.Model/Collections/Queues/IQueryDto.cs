using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// DTO specifying criteria for dequeuing messages from a queue.
/// </summary>
public interface IQueryDto
	: IDto
{
	/// <summary>
	/// Gets or sets the maximum number of messages to return.
	/// </summary>
	int MaxCount { get; set; }
	/// <summary>
	/// Gets or sets the visibility timeout after dequeuing, delaying reappearance of messages.
	/// </summary>
	TimeSpan NextVisible { get; set; }
	/// <summary>
	/// Gets or sets an optional priority filter; only messages with this priority are returned.
	/// </summary>
	int? Priority { get; set; }
	/// <summary>
	/// Gets or sets the queue name to query.
	/// </summary>
	string Queue { get; set; }
}

