using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Adds a primary key constraint to a database column.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE ADD CONSTRAINT DDL statement
/// to add a primary key constraint to an existing column. The primary key is created as a
/// clustered index by default, which physically orders the table data according to the
/// primary key column values. The constraint name is automatically generated using a
/// consistent naming convention. The index is created on the specified filegroup for
/// optimal storage allocation. Primary keys enforce uniqueness and non-nullability on
/// the specified column and serve as the default clustering key for the table.
/// </remarks>
internal class PrimaryKeyAdd(ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the ALTER TABLE ADD CONSTRAINT PRIMARY KEY statement.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for adding the primary key constraint.
	/// </summary>
	/// <value>
	/// The ALTER TABLE ADD CONSTRAINT PRIMARY KEY statement with filegroup specification.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");
			text.AppendLine($"ADD CONSTRAINT {Context.GenerateConstraintName(Context.Schema.Schema, Context.Schema.Name, ConstraintNameType.PrimaryKey)}");
			text.AppendLine($"PRIMARY KEY CLUSTERED ({Escape(Column.Name)}) ON {Escape(SchemaExtensions.FileGroup)}");

			return text.ToString();
		}
	}
}
