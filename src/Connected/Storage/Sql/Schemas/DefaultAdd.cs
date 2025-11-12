using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Adds a default constraint to a database column.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE ADD CONSTRAINT DDL statement
/// to add a default constraint to an existing column. The constraint name is automatically
/// generated using a consistent naming convention. The default value is parsed and formatted
/// appropriately for the SQL dialect before being included in the DDL statement.
/// </remarks>
internal class DefaultAdd(ISchemaColumn column, string tableName)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the ALTER TABLE ADD CONSTRAINT statement to add the default constraint.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for adding the default constraint.
	/// </summary>
	/// <value>
	/// The ALTER TABLE ADD CONSTRAINT DEFAULT statement with the parsed default value.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			/*
			 * Parse the default value to ensure proper SQL formatting.
			 */
			var defValue = SchemaExtensions.ParseDefaultValue(Column.DefaultValue);

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, tableName)}");
			text.AppendLine($"ADD CONSTRAINT {Context.GenerateConstraintName(Context.Schema.Schema, tableName, ConstraintNameType.Default)} DEFAULT {defValue} FOR {Column.Name}");

			return text.ToString();
		}
	}
}
