using Connected.Services;

namespace Connected.Collections.Queues;

public interface IQueueAction<TDto>
	where TDto : IDto
{
	/// <summary>
	/// Invokes the queue client to process the message.
	/// </summary>
	/// <param name="message">The queue message containing the DTO and related information.</param>
	/// <param name="cancel">A cancellation token used to abort processing.</param>
	Task Invoke(IQueueMessage message, CancellationToken cancel = default);
}