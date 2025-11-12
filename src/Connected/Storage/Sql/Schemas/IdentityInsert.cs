using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Enables or disables IDENTITY_INSERT for a table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the SET IDENTITY_INSERT DDL statement to control
/// whether explicit values can be inserted into an identity column. When IDENTITY_INSERT is ON,
/// explicit values can be inserted into the identity column, bypassing the automatic value
/// generation. This is essential during data migration operations, table recreation scenarios,
/// or when copying data with preserved identity values. Only one table per session can have
/// IDENTITY_INSERT enabled at a time. The operation must be turned OFF after completing the
/// explicit inserts to restore normal identity column behavior.
/// </remarks>
internal class IdentityInsert(string tableName, bool on)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the SET IDENTITY_INSERT statement to enable or disable explicit
		 * value insertion into identity columns.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for setting IDENTITY_INSERT.
	/// </summary>
	/// <value>
	/// The SET IDENTITY_INSERT statement with ON or OFF switch.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();
			var switchCommand = on ? "ON" : "OFF";

			text.AppendLine($"SET IDENTITY_INSERT {Escape(Context.Schema.Schema, tableName)}  {switchCommand}");

			return text.ToString();
		}
	}
}
