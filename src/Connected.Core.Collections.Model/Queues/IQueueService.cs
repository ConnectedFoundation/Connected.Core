using Connected.Annotations;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
/// <summary>
/// Represents a background service for processing queue messages.
/// </summary>
/// <remarks>
/// Queue mechanism is mostly used as an internal logic of processes
/// and resources to offload work from the main thread to achieve better
/// responsiveness of the system. Aggregations and calculations are good
/// examples of queue usage. You should use queue whenever you must
/// perform any kind of work that is not necessary to perform it in a single
/// transaction scope.
/// The service is not directly accessible from external clients because the
/// preferred way is to enqueue messages as a part of Service operation internal logic.
/// </remarks>
[Service]
public interface IQueueService
{
	/// <summary>
	/// Inserts the queue message.
	/// </summary>
	/// <typeparam name="TDto">The type of the dto used in queue message</typeparam>
	/// <param name="dto">The dto containing information about a queue message.</param>
	/// <param name="options">The options for the queue message.</param>
	Task Insert<TClient, TDto>(TDto dto, IInsertOptionsDto options)
		where TClient : IQueueClient<TDto>
		where TDto : IDto;
	/// <summary>
	/// Dequeues the queue messages based on the provided criteria.
	/// </summary>
	/// <param name="dto">The dto containing information about dequeue criteria.</param>
	/// <returns>A list of valid queue messages that can be immediatelly processed.</returns>
	Task<ImmutableList<IQueueMessage>> Query(IQueryDto dto);
	/// <summary>
	/// Updates the existing queue message with the specified pop receipt.
	/// </summary>
	/// <param name="dto">The dto containing information about queue message.</param>
	Task Update(IUpdateDto dto);
	/// <summary>
	/// Deleted the existing queue message with the specified pop receipt.
	/// </summary>
	/// <param name="dto">The dto containing information about queue message.</param>
	Task Delete(IValueDto<Guid> dto);
}
