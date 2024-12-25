using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// The model of the queue client.
/// </summary>
/// <remarks>
/// Each queue message has a client atztached when inserted into IQueueService. The
/// client is invoked once the message is retrieved by a host and ready for processing.
/// </remarks>
public interface IQueueClient<TDto> : IMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Invokes the queue client to process the message.
	/// </summary>
	/// <param name="message">The queue message containing the dto 
	/// and other message related information.</param>
	Task Invoke(IQueueMessage message, CancellationToken cancel = default);
}
