using Connected.Services;

namespace Connected.Collections.Queues;

public static class QueueExtensions
{
	public static async Task Debounce<TClient>(this IQueueService queueService, string queue, int id)
				where TClient : IQueueClient<IPrimaryKeyDto<int>>
	{
		await queueService.Debounce<TClient, int>(queue, id, TimeSpan.FromSeconds(5));
	}

	public static async Task Debounce<TClient>(this IQueueService queueService, string queue, long id)
			where TClient : IQueueClient<IPrimaryKeyDto<long>>
	{
		await queueService.Debounce<TClient, long>(queue, id, TimeSpan.FromSeconds(5));
	}

	public static async Task Debounce<TClient>(this IQueueService queueService, string queue, int id, TimeSpan delay)
				where TClient : IQueueClient<IPrimaryKeyDto<int>>
	{
		await queueService.Debounce<TClient, int>(queue, id, delay);
	}

	public static async Task Debounce<TClient>(this IQueueService queueService, string queue, long id, TimeSpan delay)
			where TClient : IQueueClient<IPrimaryKeyDto<long>>
	{
		await queueService.Debounce<TClient, long>(queue, id, delay);
	}

	public static async Task Debounce<TClient>(this IQueueService queueService, string queue, int id, TimeSpan delay, TimeSpan? timeout)
			where TClient : IQueueClient<IPrimaryKeyDto<int>>
	{
		await queueService.Debounce<TClient, int>(queue, id, delay, timeout);
	}

	public static async Task Debounce<TClient>(this IQueueService queueService, string queue, long id, TimeSpan delay, TimeSpan? timeout)
			where TClient : IQueueClient<IPrimaryKeyDto<long>>
	{
		await queueService.Debounce<TClient, long>(queue, id, delay, timeout);
	}

	private static async Task Debounce<TClient, TValue>(this IQueueService queueService, string queue, TValue id, TimeSpan delay)
				where TClient : IQueueClient<IPrimaryKeyDto<TValue>>
	{
		await Debounce<TClient, TValue>(queueService, queue, id, delay, null);
	}

	private static async Task Debounce<TClient, TValue>(this IQueueService queueService, string queue, TValue id, TimeSpan delay, TimeSpan? timeout)
				where TClient : IQueueClient<IPrimaryKeyDto<TValue>>
	{
		var options = new Dto<IInsertOptionsDto>().Value;

		options.Expire = DateTimeOffset.UtcNow.AddHours(8);
		options.Batch = id!.ToString();
		options.NextVisible = DateTimeOffset.UtcNow.Add(delay);
		options.Queue = queue;
		options.MaxDequeueCount = 10;
		options.Priority = 0;
		options.BatchTimeout = timeout;

		await queueService.Insert<TClient, IPrimaryKeyDto<TValue>>(Dto.Factory.CreatePrimaryKey(id), options);
	}
}
