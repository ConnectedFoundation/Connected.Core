using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// The default implementation of the queue client.
/// </summary>
public abstract class QueueClient<TDto> : Middleware, IQueueClient<TDto>
	where TDto : IDto
{
	/// <summary>
	/// Gets the queue message which is proccesed by a client.
	/// </summary>
	protected IQueueMessage Message { get; private set; } = default!;
	/// <summary>
	/// A Dto object containing the necessary information for retrieving the data to
	/// be processed.
	/// </summary>
	protected TDto Dto { get; private set; } = default!;
	/// <summary>
	/// A cancellation token that should be used for determining if the request has been cancelled.
	/// </summary>
	protected CancellationToken Cancel { get; private set; }
	/// <summary>
	/// Starts the processing of the queue message.
	/// </summary>
	/// <param name="message">The queue message that a client should process.</param>
	/// <param name="cancel">A cancellation token that should be used for determining if the request has been cancelled.</param>
	public async Task Invoke(IQueueMessage message, CancellationToken cancel = default)
	{
		Message = message;
		Dto = (TDto)message.Dto;
		Cancel = cancel;

		await OnInvoke();
	}
	/// <summary>
	/// Process the queue message.
	/// </summary>
	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}