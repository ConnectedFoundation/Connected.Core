using Connected.Services;
using Connected.Workers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
/// <summary>
/// The base class for implementing a queue host.
/// </summary>
public abstract class QueueHost : ScheduledWorker
{
	/// <summary>
	/// Create a new instance of the QueueHost class.
	/// </summary>
	/// <param name="queue">The name of the queue this host will process.</param>
	/// <param name="size">The maximum number of threads that will be reserved for this host.</param>
	public QueueHost(string queue, int size)
	{
		Queue = queue;
		Timer = TimeSpan.FromMilliseconds(500);

		Dispatcher = new();

		Dispatcher.WorkerSize = size;
	}

	private QueueDispatcher Dispatcher { get; }
	private string Queue { get; }
	/// <summary>
	/// The interval which will be set from now when dequeued messages
	/// will become visible again.
	/// </summary>
	protected TimeSpan NextVisibleInterval { get; set; } = TimeSpan.FromSeconds(30);

	protected override sealed async Task OnInvoke(CancellationToken cancel)
	{
		using var scope = Scope.Create();
		var service = scope.ServiceProvider.GetRequiredService<IQueueService>();

		try
		{
			var items = await service.Query(new QueryDto
			{
				MaxCount = Dispatcher.Available,
				NextVisible = NextVisibleInterval,
				Queue = Queue,
				Priority = Dispatcher.MinPriority
			});

			items = await OnDequeued(items);

			if (!items.Any())
				return;
			/*
			 * Make sure changes to the queue service get commited.
			 */
			await scope.Commit();
			/*
			 * Messages have been receive but it's not necessary we'll process them
			 */
			foreach (var item in items)
			{
				/*
				 * If the host won't accept the message it will be returned to the queue.
				 */
				if (!await Accept(Dispatcher.Queue, item))
				{
					await service.Update(new UpdateDto { NextVisible = TimeSpan.Zero, Value = item.PopReceipt.GetValueOrDefault() });
					continue;
				}
				/*
				 * It's accepted. Enqueue it which will trigger the invoke on the queue client.
				 */
				Dispatcher.Enqueue(item);
			}
		}
		catch
		{
			await scope.Rollback();

			throw;
		}
	}

	protected virtual async Task<bool> Accept(ConcurrentQueue<IQueueMessage> queue, IQueueMessage message)
	{
		return await Task.FromResult(true);
	}

	protected virtual async Task<IImmutableList<IQueueMessage>> OnDequeued(IImmutableList<IQueueMessage> messages)
	{
		return await Task.FromResult(messages);
	}
}