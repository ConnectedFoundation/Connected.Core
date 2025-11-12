using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Creates a new database table with column definitions, constraints, and indexes.
/// </summary>
/// <remarks>
/// This transaction generates and executes the CREATE TABLE DDL statement along with subsequent
/// statements to add primary keys, default constraints, and indexes. The operation supports both
/// permanent and temporary table creation, with temporary tables being assigned generated names
/// for use during table recreation scenarios. After creating the table structure, the transaction
/// orchestrates the creation of all associated database objects including primary key constraints,
/// default value constraints, and indexes. Temporary tables skip constraint and index creation
/// to optimize performance during intermediate operations.
/// </remarks>
internal class TableCreate
	: TableTransaction
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TableCreate"/> class.
	/// </summary>
	/// <param name="temporary">Indicates whether to create a temporary table.</param>
	/// <remarks>
	/// If creating a temporary table, a unique name is generated using a GUID to avoid
	/// naming conflicts with existing database objects.
	/// </remarks>
	public TableCreate(bool temporary)
	{
		Temporary = temporary;

		if (Temporary)
			TemporaryName = $"T{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
	}

	/// <summary>
	/// Gets a value indicating whether this is a temporary table.
	/// </summary>
	private bool Temporary { get; }

	/// <summary>
	/// Gets the name of the temporary table if applicable.
	/// </summary>
	/// <value>
	/// The generated temporary table name, or <c>null</c> if creating a permanent table.
	/// </value>
	public string? TemporaryName { get; }

	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the CREATE TABLE statement to create the table structure.
		 */
		await Context.Execute(CommandText);

		/*
		 * For permanent tables, create all associated constraints and indexes.
		 * Temporary tables skip this step for performance optimization.
		 */
		if (!Temporary)
		{
			await ExecutePrimaryKey();
			await ExecuteDefaults();
			await ExecuteIndexes();
		}
	}

	/// <summary>
	/// Creates the primary key constraint for the table.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task ExecutePrimaryKey()
	{
		var primaryKey = Context.Schema.Columns.FirstOrDefault(f => f.IsPrimaryKey);

		if (primaryKey is not null)
			await new PrimaryKeyAdd(primaryKey).Execute(Context);
	}

	/// <summary>
	/// Creates default value constraints for columns that specify defaults.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="NullReferenceException">Thrown when temporary name is null for a temporary table.</exception>
	private async Task ExecuteDefaults()
	{
		var name = Temporary ? TemporaryName ?? throw new NullReferenceException(SR.ErrExpectedTemporaryName) : Context.Schema.Name;

		/*
		 * Add default constraints for all columns that have default values defined.
		 */
		foreach (var column in Context.Schema.Columns)
		{
			if (!string.IsNullOrWhiteSpace(column.DefaultValue))
				await new DefaultAdd(column, name).Execute(Context);
		}
	}

	/// <summary>
	/// Creates indexes for the table based on column index attributes.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task ExecuteIndexes()
	{
		var indexes = ParseIndexes(Context.Schema);

		foreach (var index in indexes)
			await new IndexCreate(index).Execute(Context);
	}

	/// <summary>
	/// Gets the DDL command text for creating the table.
	/// </summary>
	/// <value>
	/// The CREATE TABLE statement with all column definitions.
	/// </value>
	/// <exception cref="NullReferenceException">Thrown when temporary name is null for a temporary table.</exception>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			var name = Temporary ? TemporaryName ?? throw new NullReferenceException(SR.ErrExpectedTemporaryName) : Context.Schema.Name;

			text.AppendLine($"CREATE TABLE {Escape(Context.Schema.Schema, name)}");
			text.AppendLine("(");
			var comma = string.Empty;

			/*
			 * Add column definitions to the CREATE TABLE statement.
			 */
			for (var i = 0; i < Context.Schema.Columns.Count; i++)
			{
				text.AppendLine($"{comma} {CreateColumnCommandText(Context.Schema.Columns[i])}");

				comma = ",";
			}

			text.AppendLine(");");

			return text.ToString();
		}
	}
}
