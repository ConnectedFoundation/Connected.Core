namespace Connected.Storage.Sql.Schemas;

internal abstract class SynchronizationTransaction
	: SynchronizationCommand
{
	protected SchemaExecutionContext Context { get; private set; } = default!;

	public async Task Execute(SchemaExecutionContext context)
	{
		Context = context;

		await OnExecute();
	}

	protected virtual async Task OnExecute()
	{
		await Task.CompletedTask;
	}
}
