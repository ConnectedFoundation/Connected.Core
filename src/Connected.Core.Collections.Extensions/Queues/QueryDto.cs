using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Collections.Queues;
/// <summary>
/// Dto object used when querying queue messages (dequeueing).
/// </summary>
public sealed class QueryDto : Dto, IQueryDto
{
	/// <summary>
	/// The maximum number of messages that will be returned for this query.
	/// </summary>
	[Range(1, int.MaxValue)]
	public int MaxCount { get; set; }
	/// <summary>
	/// The period with which returned messages will move the NextVisible value.
	/// </summary>
	/// <remarks>
	/// Do not set this value to very short internal, for example to less than a second since
	/// it's very likely that the message won't get processes soon enough before gets available
	/// again.
	/// </remarks>
	public TimeSpan NextVisible { get; set; } = TimeSpan.FromSeconds(30);
	/// <summary>
	/// The minimum priority to return.
	/// </summary>
	/// <remarks>
	/// IQueueService returns only one set of priorities for each request. 
	/// IQueueService immediately returns once it find a message with a priority
	/// different than the one already in a result set.
	/// </remarks>
	public int? Priority { get; set; }
	/// <summary>
	/// The queue from which messages to return.
	/// </summary>
	[Required, MaxLength(128)]
	public string Queue { get; set; } = default!;
}
