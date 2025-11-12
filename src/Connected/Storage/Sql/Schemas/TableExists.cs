using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Checks whether a database table exists.
/// </summary>
/// <remarks>
/// This query transaction executes a query against the INFORMATION_SCHEMA.TABLES system view
/// to determine whether a table with the specified schema and name exists in the database.
/// The operation is used during schema synchronization to decide whether to create a new table
/// or modify an existing one. The query returns a boolean result indicating table existence,
/// enabling conditional schema operations based on the current database state.
/// </remarks>
internal class TableExists
	: SynchronizationQuery<bool>
{
	/// <inheritdoc/>
	protected override async Task<bool> OnExecute()
	{
		/*
		 * Execute the existence check query and retrieve the result.
		 */
		var result = await Context.Select(CommandText);

		if (result is null)
			return false;

		return result.Result;
	}

	/// <summary>
	/// Gets the SQL command text for checking table existence.
	/// </summary>
	/// <value>
	/// A SQL query that checks INFORMATION_SCHEMA.TABLES and returns 1 if the table exists, 0 otherwise.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"IF (EXISTS (SELECT *  FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = '{Unescape(Context.Schema.Schema)}' AND TABLE_NAME = '{Unescape(Context.Schema.Name)}')) SELECT 1 as result ELSE SELECT 0 as result");

			return text.ToString();
		}
	}
}
