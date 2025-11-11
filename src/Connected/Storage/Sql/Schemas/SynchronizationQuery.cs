namespace Connected.Storage.Sql.Schemas;

internal abstract class SynchronizationQuery<T> : SynchronizationCommand
{
	protected SchemaExecutionContext Context { get; private set; } = default!;

	public async Task<T> Execute(SchemaExecutionContext context)
	{
		Context = context;

		return await OnExecute();
	}

	protected abstract Task<T> OnExecute();
}
