using Connected.Storage.Schemas;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Alters an existing column in an Oracle database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes ALTER TABLE MODIFY DDL statements to modify
/// an existing column's definition. Oracle uses the MODIFY clause to change column properties
/// such as data type, size, nullability, and default values. Unlike PostgreSQL which requires
/// separate ALTER COLUMN statements for each property, Oracle allows multiple modifications
/// in a single MODIFY clause. The operation determines which properties have changed and generates
/// an appropriate ALTER TABLE MODIFY statement. Oracle automatically handles data type conversions
/// when possible, but may fail if existing data is incompatible with the new definition. Identity
/// column modifications require Oracle 12c+ and may have restrictions depending on the generation type.
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
		 * Oracle uses MODIFY clause for column alterations.
		 * Build the complete column definition including type, nullability, and default.
		 */
		var columnDefinition = CreateColumnCommandText(Column);

		/*
		 * Execute ALTER TABLE MODIFY statement.
		 * Oracle allows modifying multiple attributes in a single statement.
		 */
		await Context.Execute($"ALTER TABLE {tableName} MODIFY ({columnDefinition})");
	}
}
