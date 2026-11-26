using Connected.Storage.Schemas;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Alters an existing column in a PostgreSQL database table.
/// </summary>
/// <remarks>
/// The statements come from <see cref="SynchronizationCommand.AlterColumnCommands"/>: the column is altered to the definition of
/// the schema column as a whole (type, nullability and default), in one ALTER TABLE statement, so a change the server refuses
/// leaves the column as it was. The caller has already decided that the column differs, this transaction does not compare
/// properties. The table is not recreated.
/// </remarks>
internal sealed class ColumnAlter(ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		var tableName = Escape(Context.Schema.Schema, Context.Schema.Name);

		foreach (var command in AlterColumnCommands(tableName, Column))
			await Context.Execute(command);
	}
}
