using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Alters an existing column in a PostgreSQL database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes ALTER TABLE ALTER COLUMN DDL statements to modify
/// an existing column's definition. PostgreSQL requires separate ALTER COLUMN statements for
/// different column properties such as data type, nullability, and default values. The operation
/// determines which properties have changed and generates appropriate ALTER statements for each
/// modification. This approach ensures that only necessary changes are applied while preserving
/// existing data where possible. Identity column modifications may require special handling
/// or table recreation in some cases.
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
