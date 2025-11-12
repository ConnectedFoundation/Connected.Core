using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Adds a new column to an existing PostgreSQL database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE ADD COLUMN DDL statement for adding
/// a new column to a table. After adding the column, it handles additional operations such as
/// primary key constraint creation if specified in the column definition. PostgreSQL supports
/// adding columns with default values and identity specifications in a single ALTER TABLE statement,
/// simplifying the operation compared to other database systems. The operation ensures that all
/// column characteristics including constraints are properly applied during the column addition process.
/// </remarks>
internal sealed class ColumnAdd(ISchemaColumn column)
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

			text.Append($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} ");
			text.Append($"ADD COLUMN {CreateColumnCommandText(Column)}");

			return text.ToString();
		}
	}
}
