using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Adds a new column to an existing database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE ADD COLUMN DDL statement for adding
/// a new column to a table. After adding the column, it handles additional operations such as
/// primary key constraint creation and default value assignment if specified in the column definition.
/// The operation ensures that all column characteristics including constraints and defaults are
/// properly applied during the column addition process.
/// </remarks>
internal class ColumnAdd(ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the ALTER TABLE ADD COLUMN statement to add the new column.
		 */
		await Context.Execute(CommandText);

		/*
		 * If the column is designated as a primary key, create the primary key constraint.
		 */
		if (Column.IsPrimaryKey)
			await new PrimaryKeyAdd(Column).Execute(Context);

		/*
		 * If a default value is specified, add the default constraint to the column.
		 */
		if (!string.IsNullOrWhiteSpace(Column.DefaultValue))
			await new DefaultAdd(Column, Context.Schema.Name).Execute(Context);
	}

	/// <summary>
	/// Gets the DDL command text for adding the column.
	/// </summary>
	/// <value>
	/// The ALTER TABLE ADD COLUMN statement with column definition.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");
			text.AppendLine($"ADD COLUMN {CreateColumnCommandText(Column)}");

			return text.ToString();
		}
	}
}
