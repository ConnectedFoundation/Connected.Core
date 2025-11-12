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
public interface IQueueClient<TDto>
	: IMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Invokes the queue client to process the message.
	/// </summary>
	/// <param name="message">The queue message containing the DTO and related information.</param>
	/// <param name="cancel">A cancellation token used to abort processing.</param>
	Task Invoke(IQueueMessage message, CancellationToken cancel = default);
}
