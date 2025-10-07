using Connected.Annotations;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Collections.Queues;
/// <summary>
/// A Dto providing options when inserting queue message.
/// </summary>
internal sealed class InsertOptionsDto : Dto
{
	/// <summary>
	/// The date and time the queue message expires.
	/// </summary>
	/// <remarks>
	/// Queue messages that are not processed until they expire
	/// gets automatically deleted by the system. Defaults to 48 hours.
	/// </remarks>
	public DateTimeOffset Expire { get; set; } = DateTimeOffset.UtcNow.AddHours(48);
	/// <summary>
	/// An optional batch which acts as a filter for improving the performance.
	/// </summary>
	/// <remarks>
	/// If this property is set to a non null value, the IQueueService will ignore
	/// this message if it already exists for the combination of Batch, Client and Queue.
	/// Client is set as an type argument of the Insert operation.
	/// </remarks>
	[MaxLength(256)]
	public string? Batch { get; set; }
	/// <summary>
	/// The message priority. Higher priority means the message will be dequeued sooner
	/// than messages with lower priority.
	/// </summary>
	public int Priority { get; set; }
	/// <summary>
	/// An optional timestamp when the message will be first visible the the IQueueService.
	/// </summary>
	/// <remarks>
	/// Message won't get dequeued until this value is past DateTimeOffset.UtcNow value.
	/// </remarks>
	public DateTimeOffset? NextVisible { get; set; } = DateTime.UtcNow;
	/// <summary>
	/// Queue identifier to which this message belongs. Each QueueHost should process only
	/// one queue.
	/// </summary>
	[Required, MaxLength(128)]
	public string Queue { get; set; } = default!;
	/// <summary>
	/// Maximum number of dequeue tries that will be allowed before the message will be automatically
	/// deleted
	/// </summary>
	[MinValue(1)]
	public int MaxDequeueCount { get; set; } = 10;
}
