using Connected.Authentication;
using Connected.Services;
using Connected.Workers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
/// <summary>
/// The base class for implementing a queue host.
/// </summary>
public abstract class QueueHost<TEntity, TCache>
	: ScheduledWorker
	where TEntity : IQueueMessage
	where TCache : IQueueMessageCache<TEntity>
{
	/// <summary>
	/// Create a new instance of the QueueHost class.
	/// </summary>
	/// <param name="size">The maximum number of threads that will be reserved for this host.</param>
	public QueueHost(int size)
	{
		Timer = TimeSpan.FromMilliseconds(500);

		Dispatcher = new()
		{
			WorkerSize = size
		};
	}
	public QueueHost()
		: this(1)
	{

	}
	private QueueDispatcher<TEntity, TCache> Dispatcher { get; }
	/// <summary>
	/// The interval which will be set from now when dequeued messages
	/// will become visible again.
	/// </summary>
	protected TimeSpan NextVisibleInterval { get; set; } = TimeSpan.FromSeconds(30);

	protected override sealed async Task OnInvoke(CancellationToken cancel)
	{
		if (Dispatcher.Available <= 0)
			return;

		using var scope = await Scope.Create().WithSystemIdentity();
		var service = scope.ServiceProvider.GetRequiredService<IQueueService>();

		try
		{
			var items = await service.Query<TEntity, TCache>(new QueryDto
			{
				MaxCount = Dispatcher.Available,
				NextVisible = NextVisibleInterval,
				Priority = Dispatcher.MinPriority
			});

			items = await OnDequeued(items);
			/*
			 * Make sure changes to the queue service get commited.
			 */
			await scope.Commit();

			if (!items.Any())
				return;
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
					await service.Update<TEntity, TCache>(new UpdateDto { NextVisible = TimeSpan.Zero, Value = item.PopReceipt.GetValueOrDefault() });
					continue;
				}
				/*
				 * It's accepted. Enqueue it which will trigger the invoke on the queue client.
				 */
				await Dispatcher.Enqueue(item);
			}
		}
		catch
		{
			await scope.Rollback();

			throw;
		}
	}

	protected virtual async Task<bool> Accept(ConcurrentQueue<TEntity> queue, IQueueMessage message)
	{
		return await Task.FromResult(true);
	}

	protected virtual async Task<IImmutableList<TEntity>> OnDequeued(IImmutableList<TEntity> messages)
	{
		return await Task.FromResult(messages);
	}
}