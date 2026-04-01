using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// The model of the queue client.
/// </summary>
/// <remarks>
/// Each queue message has a client attached when inserted into <c>IQueueService</c>. The
/// client is invoked once the message is retrieved by a host and ready for processing.
/// </remarks>
/// <typeparam name="TDto">The DTO type processed by the queue client.</typeparam>
public interface IQueueContext<TAction, TDto>
	where TAction : IQueueAction<TDto>
	where TDto : IDto
{
	Task Invoke(TDto dto);
}
