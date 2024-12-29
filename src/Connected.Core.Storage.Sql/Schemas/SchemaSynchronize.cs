namespace Connected.Storage.Sql.Schemas;

internal class SchemaSynchronize : SynchronizationTransaction
{
	protected override async Task OnExecute()
	{
		if (string.IsNullOrWhiteSpace(Context.Schema.Schema))
			return;

		if (!await new SchemaExists(Context.Schema.Schema).Execute(Context))
			await CreateSchema();
	}

	private async Task CreateSchema()
	{
		await Context.Execute($"CREATE SCHEMA {Context.Schema.Schema};");
	}
}
