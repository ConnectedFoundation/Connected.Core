using Connected.Entities;
using Connected.Services;
using Connected.Storage;

namespace Connected.Collections.Queues;

internal sealed class Insert<TClient, TDto>(IQueueCache cache, IStorageProvider storage) : ServiceAction<TDto>
		where TClient : IQueueClient<TDto>
		where TDto : IDto
{
	private IStorageProvider Storage { get; } = storage;
	public IInsertOptionsDto Options { get; set; } = default!;

	protected override async Task OnInvoke()
	{
		if (!await Validate())
			return;

		var dtoTypeName = Dto.GetType().FullName ?? throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} '{Dto.GetType()}'");

		var message = new QueueMessage
		{
			Dto = Dto,
			DtoTypeName = $"{dtoTypeName}, {Dto.GetType().Assembly.GetName().Name}",
			Created = DateTime.UtcNow,
			Client = typeof(TClient),
			Batch = Options.Batch,
			NextVisible = Options.NextVisible ?? DateTimeOffset.UtcNow,
			Priority = Options.Priority,
			Expire = Options.Expire,
			Queue = Options.Queue,
			State = State.Add,
			MaxDequeueCount = Options.MaxDequeueCount
		};

		var storage = Storage.Open<QueueMessage>(StorageConnectionMode.Isolated);

		message = await storage.Update(message);

		if (message is null)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		message = await storage.Where(f => f.Id == message.Id).AsEntity();

		if (message is null)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		await cache.Update(message);
	}

	private async Task<bool> Validate()
	{
		/*
		 * This is performance optimization.
		 * We only enqueue one message with the same batch argument 
       * at the same time if the batch has been specified.
		 */
		if (string.IsNullOrWhiteSpace(Options.Batch))
			return true;

		if (await cache.Exists(typeof(TClient), Options.Batch))
			return false;

		return true;
	}
}