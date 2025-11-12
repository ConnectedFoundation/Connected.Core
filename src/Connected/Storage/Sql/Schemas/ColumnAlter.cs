using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Alters an existing database column to match a new definition.
/// </summary>
/// <remarks>
/// This transaction handles column modifications by comparing the existing column definition
/// with the desired column definition and executing the necessary DDL operations to align them.
/// It manages complex scenarios including data type changes, nullability changes, default value
/// modifications, and primary key constraint alterations. The operation follows a specific
/// sequence: drop existing defaults, alter column structure, add new defaults, and manage
/// primary key constraints to ensure referential integrity is maintained throughout the process.
/// </remarks>
internal class ColumnAlter(ISchemaColumn column, ExistingSchema existing, ISchemaColumn existingColumn)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Compare columns and exit early if no changes are detected.
		 */
		if (ColumnComparer.Default.Equals(existingColumn, Column))
			return;

		/*
		 * Drop existing default constraint if it exists and differs from the new default.
		 */
		if (!string.IsNullOrWhiteSpace(existingColumn.DefaultValue))
		{
			var existingDefault = SchemaExtensions.ParseDefaultValue(existingColumn.DefaultValue);
			var def = SchemaExtensions.ParseDefaultValue(Column.DefaultValue);

			if (!string.Equals(existingDefault, def, StringComparison.Ordinal))
				await new DefaultDrop(Column).Execute(Context);
		}

		/*
		 * Execute the ALTER COLUMN statement if structural properties have changed.
		 * This includes data type, nullability, maximum length, or version tracking changes.
		 */
		if (Column.DataType != existingColumn.DataType
			|| Column.IsNullable != existingColumn.IsNullable
			|| Column.MaxLength != existingColumn.MaxLength
			|| Column.IsVersion != existingColumn.IsVersion)
			await Context.Execute(CommandText);

		/*
		 * Add new default constraint if the default value has changed and a new one is specified.
		 */
		var ed = SchemaExtensions.ParseDefaultValue(existingColumn.DefaultValue);
		var nd = SchemaExtensions.ParseDefaultValue(Column.DefaultValue);

		if (!string.Equals(ed, nd, StringComparison.Ordinal) && nd is not null)
			await new DefaultAdd(Column, Context.Schema.Name).Execute(Context);

		/*
		 * Handle primary key constraint changes by adding or removing as needed.
		 */
		if (!existingColumn.IsPrimaryKey && Column.IsPrimaryKey)
			await new PrimaryKeyAdd(Column).Execute(Context);
		else if (existingColumn.IsPrimaryKey && !Column.IsPrimaryKey)
			await new PrimaryKeyRemove(existing, existingColumn).Execute(Context);
	}

	/// <summary>
	/// Gets the DDL command text for altering the column.
	/// </summary>
	/// <value>
	/// The ALTER TABLE ALTER COLUMN statement with new column definition.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");
			text.AppendLine($"ALTER COLUMN {CreateColumnCommandText(Column)}");

			return text.ToString();
		}
	}
}
