using Connected.Annotations;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
/// <summary>
/// Represents a background service for processing queue messages.
/// </summary>
/// <remarks>
/// Queue mechanism is mostly used as internal process logic and resource orchestration to offload work from the main thread
/// to achieve better responsiveness of the system. Aggregations and calculations are common uses of queues. Use a queue when you must
/// perform work that is not necessary to perform in a single transaction scope. The service is not directly accessible from external clients;
/// the preferred approach is to enqueue messages as part of a service operation's internal logic.
/// </remarks>
[Service]
public interface IQueueService
{
	/// <summary>
	/// Inserts the queue message.
	/// </summary>
	/// <typeparam name="TClient">The client type that will process the message.</typeparam>
	/// <typeparam name="TDto">The type of the DTO used in the queue message.</typeparam>
	/// <param name="dto">The DTO containing information about a queue message.</param>
	/// <param name="options">The options for the queue message.</param>
	Task Insert<TClient, TDto>(TDto dto, IInsertOptionsDto options)
		where TClient : IQueueClient<TDto>
		where TDto : IDto;
	/// <summary>
	/// Dequeues the queue messages based on the provided criteria.
	/// </summary>
	/// <param name="dto">The DTO containing information about dequeue criteria.</param>
	/// <returns>A list of valid queue messages that can be immediately processed.</returns>
	Task<IImmutableList<IQueueMessage>> Query(IQueryDto dto);
	/// <summary>
	/// Updates the existing queue message with the specified pop receipt.
	/// </summary>
	/// <param name="dto">The DTO containing information about the queue message.</param>
	Task Update(IUpdateDto dto);
	/// <summary>
	/// Deletes the existing queue message with the specified pop receipt.
	/// </summary>
	/// <param name="dto">The DTO containing information about the queue message.</param>
	Task Delete(IValueDto<Guid> dto);
}
