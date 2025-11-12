using Connected.Collections.Concurrent;
using Connected.Reflection;
using Connected.Services;
using Connected.Threading;
using Microsoft.Extensions.Logging;

namespace Connected.Collections.Queues;

internal sealed class QueueWorker(IQueueService queue, ILogger<QueueWorker> logger)
	: DispatcherJob<IQueueMessage>
{
	private TaskTimeout? Timeout { get; set; }

	protected override async Task OnInvoke()
	{
		/*
		 * Can be old messages with no runtime type available anymore.
		 */
		if (Dto.Client is null)
		{
			await Complete();

			return;
		}
		/*
		 * We must first ensure we have enough for processing so
		 * we'll perform a ping operation on a message to extend
		 * next visible time.
		 */
		await queue.Update(new UpdateDto
		{
			Value = Dto.PopReceipt.GetValueOrDefault(),
			NextVisible = TimeSpan.FromSeconds(60)
		});
		/*
		 * Now, this is a guard task which ensures that even if the queue
		 * runs longer than 60 seconds it won't get dequeued again.
		 */
		Timeout = new TaskTimeout(async () =>
		{
			/*
			 * It will move next visible for another 60 seconds.
			 * We are going to do it every 20 seconds.
			 */
			await queue.Update(new UpdateDto
			{
				Value = Dto.PopReceipt.GetValueOrDefault(),
				NextVisible = TimeSpan.FromSeconds(60)
			});
		}, TimeSpan.FromSeconds(20), Cancel);
		/*
		 * Start the guard.
		 */
		Timeout.Start();

		try
		{
			/*
			 * Process the message
			 */
			await Execute();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "{Message}", ex.Message);

			throw;
		}
		finally
		{
			Timeout.Stop();
			Timeout.Dispose();
		}
	}

	private async Task Execute()
	{
		/*
		 * This is just for the case of really heavy load which means that updating
		 * the queue message takes a lot of time. We simply check here if we
		 * are in danger of becoming alive again which would mean that a double (a second)
		 * message would appear in the dispatcher meaning a duplicate processing.
		 */
		if (Dto.Expire.AddSeconds(-5) < DateTimeOffset.UtcNow)
			return;

		var client = CreateClient();

		if (client is null)
		{
			await Complete();

			return;
		}

		var method = client.GetType().ResolveMethod(nameof(IQueueClient<IDto>.Invoke), null, [typeof(IQueueMessage), typeof(CancellationToken)]);

		if (method is null)
			return;

		await method.InvokeAsync(client, Dto, Cancel);
		/*
		 * At this point it is really important for the next command to execute smoothly,
		 * otherwise the message would get processed again.
		 */
		await Complete();
	}

	private async Task Complete()
	{
		await queue.Delete(Services.Dto.Factory.CreateValue(Dto.PopReceipt.GetValueOrDefault()));
	}

	private object? CreateClient()
	{
		var service = GetService(Dto.Client);

		if (service is null)
		{
			logger.LogError("Queue client service not registered ({Type})", Dto.Client.GetType().ShortName());

			return null;
		}

		if (!service.GetType().ImplementsInterface(typeof(IQueueClient<>)))
		{
			logger.LogError("Queue client does not implement IQueueClient interface ({Type})", service.GetType().ShortName());

			return null;
		}

		return service;
	}
}