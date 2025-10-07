namespace Connected.Storage.Sql.Schemas;

internal abstract class SynchronizationQuery<T> : SynchronizationCommand
{
	protected SchemaExecutionContext Context { get; private set; }

	public async Task<T> Execute(SchemaExecutionContext context)
	{
		Context = context;

		return await OnExecute();
	}

	protected virtual async Task<T> OnExecute()
	{
		await Task.CompletedTask;

		return default;
	}
}
