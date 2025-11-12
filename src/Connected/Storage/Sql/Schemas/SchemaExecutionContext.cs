using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Provides execution context for schema synchronization operations.
/// </summary>
/// <remarks>
/// This class encapsulates the runtime environment for schema transaction execution including
/// the storage provider, target schema definition, database connection information, and SQL Server
/// version. It manages constraint name generation to avoid naming conflicts, tracks existing schema
/// metadata, and provides methods for executing DDL commands and querying database metadata. The
/// context maintains a registry of constraint names used during the synchronization session to ensure
/// uniqueness. It serves as the central coordination point for all schema-related operations, providing
/// consistent access to connection resources and metadata throughout the synchronization process.
/// </remarks>
internal class SchemaExecutionContext(IStorageProvider storage, ISchema schema, string connectionString)
{
	private ExistingSchema? _existingSchema;

	/// <summary>
	/// Gets the SQL Server version from the connection string.
	/// </summary>
	/// <value>
	/// The database server version.
	/// </value>
	public Version Version { get; } = SqlStorageConnection.ResolveVersion(connectionString);

	/// <summary>
	/// Gets or sets the existing schema loaded from the database.
	/// </summary>
	/// <value>
	/// The existing schema metadata, or <c>null</c> if not loaded or the table doesn't exist.
	/// </value>
	/// <remarks>
	/// When set, this property populates the constraint registry with existing constraint names
	/// to prevent naming conflicts during schema synchronization operations.
	/// </remarks>
	public ExistingSchema? ExistingSchema
	{
		get => _existingSchema;
		set
		{
			_existingSchema = value;

			if (_existingSchema is null || _existingSchema.Descriptor is null)
				return;

			/*
			 * Register all existing constraints to avoid name collisions when
			 * generating new constraint names.
			 */
			foreach (var index in _existingSchema.Descriptor.Constraints)
			{
				if (index.Name is null)
					continue;

				switch (index.ConstraintType)
				{
					case ConstraintType.Default:
						AddConstraint(ConstraintNameType.Default, index.Name);
						break;
					case ConstraintType.PrimaryKey:
						AddConstraint(ConstraintNameType.PrimaryKey, index.Name);
						break;
					case ConstraintType.Unique:
						AddConstraint(ConstraintNameType.Index, index.Name);
						break;
				}
			}
		}
	}

	/// <summary>
	/// Gets the storage provider for executing database operations.
	/// </summary>
	public IStorageProvider Storage { get; } = storage;

	/// <summary>
	/// Gets the target schema definition to synchronize.
	/// </summary>
	public ISchema Schema { get; } = schema;

	/// <summary>
	/// Gets the registry of constraint names organized by type.
	/// </summary>
	/// <value>
	/// A dictionary mapping constraint types to lists of constraint names.
	/// </value>
	public Dictionary<ConstraintNameType, List<string>> Constraints { get; } = [];

	/// <summary>
	/// Executes a DDL command text asynchronously.
	/// </summary>
	/// <param name="commandText">The DDL command to execute.</param>
	/// <returns>A task representing the asynchronous execution operation.</returns>
	public async Task Execute(string commandText)
	{
		await Storage.Open<AdHocSchemaEntity>().Execute(new SchemaStorageDto(typeof(SqlDataConnection), connectionString, new SqlStorageOperation
		{
			CommandText = commandText
		}));
	}

	/// <summary>
	/// Executes a query and returns a single result entity.
	/// </summary>
	/// <param name="commandText">The SQL query to execute.</param>
	/// <returns>
	/// A task representing the asynchronous operation. The task result contains the query result
	/// or <c>null</c> if no result is returned.
	/// </returns>
	public async Task<AdHocSchemaEntity?> Select(string commandText)
	{
		return await Storage.Open<AdHocSchemaEntity>().Select(new SchemaStorageDto(typeof(SqlDataConnection), connectionString, new SqlStorageOperation
		{
			CommandText = commandText
		}));
	}

	/// <summary>
	/// Opens a data reader for the specified storage operation.
	/// </summary>
	/// <param name="operation">The storage operation containing the query to execute.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the data reader.</returns>
	public async Task<IDataReader> OpenReader(IStorageOperation operation)
	{
		return (await Storage.Open<AdHocSchemaEntity>().OpenReaders(new SchemaStorageDto(typeof(SqlDataConnection), connectionString, operation)))[0];
	}

	/// <summary>
	/// Adds a constraint name to the registry.
	/// </summary>
	/// <param name="type">The constraint type.</param>
	/// <param name="name">The constraint name to register.</param>
	private void AddConstraint(ConstraintNameType type, string name)
	{
		if (!Constraints.TryGetValue(type, out List<string>? existing))
		{
			existing = [];

			Constraints.Add(type, existing);
		}

		if (!ConstraintNameExists(name))
			existing.Add(name);
	}

	/// <summary>
	/// Generates a unique constraint name for the specified schema, table, and constraint type.
	/// </summary>
	/// <param name="schema">The schema name.</param>
	/// <param name="tableName">The table name.</param>
	/// <param name="type">The constraint type.</param>
	/// <returns>A unique constraint name that doesn't conflict with existing constraints.</returns>
	/// <remarks>
	/// The generated name follows the pattern: {Prefix}_{schema}_{tableName}_{index} where the index
	/// is incremented until a unique name is found.
	/// </remarks>
	public string GenerateConstraintName(string schema, string tableName, ConstraintNameType type)
	{
		var index = 0;

		/*
		 * Generate constraint names with incrementing suffix until a unique name is found.
		 */
		while (true)
		{
			var value = $"{ConstraintPrefix(type)}_{schema.ToLowerInvariant()}_{tableName}";

			if (index > 0)
				value = $"{value}_{index}";

			if (!ConstraintNameExists(value))
			{
				AddConstraint(type, value);
				return value;
			}

			index++;
		}
	}

	/// <summary>
	/// Determines whether a constraint name already exists in the registry.
	/// </summary>
	/// <param name="value">The constraint name to check.</param>
	/// <returns><c>true</c> if the constraint name exists; otherwise, <c>false</c>.</returns>
	private bool ConstraintNameExists(string value)
	{
		/*
		 * Search through all constraint types to check for name conflicts.
		 */
		foreach (var key in Constraints)
		{
			foreach (var item in key.Value)
			{
				if (item.Contains(value, StringComparison.OrdinalIgnoreCase))
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Gets the constraint name prefix for the specified constraint type.
	/// </summary>
	/// <param name="type">The constraint type.</param>
	/// <returns>The two-letter prefix for the constraint type.</returns>
	private static string ConstraintPrefix(ConstraintNameType type)
	{
		return type switch
		{
			ConstraintNameType.Default => "DF",
			ConstraintNameType.PrimaryKey => "PK",
			ConstraintNameType.Index => "IX",
			_ => "IX"
		};
	}
}
