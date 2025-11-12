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
		var columnName = Escape(Column.Name);

		/*
		 * Alter data type if needed
		 */
		await Context.Execute($"ALTER TABLE {tableName} ALTER COLUMN {columnName} TYPE {FormatType(Column.DataType, Column.MaxLength)}");

		/*
		 * Alter nullability if needed
		 */
		if (Column.IsNullable)
			await Context.Execute($"ALTER TABLE {tableName} ALTER COLUMN {columnName} DROP NOT NULL");
		else
			await Context.Execute($"ALTER TABLE {tableName} ALTER COLUMN {columnName} SET NOT NULL");

		/*
		 * Handle default value if specified
		 */
		if (!string.IsNullOrWhiteSpace(Column.DefaultValue))
			await Context.Execute($"ALTER TABLE {tableName} ALTER COLUMN {columnName} SET DEFAULT {Column.DefaultValue}");
		else
			await Context.Execute($"ALTER TABLE {tableName} ALTER COLUMN {columnName} DROP DEFAULT");
	}
}
