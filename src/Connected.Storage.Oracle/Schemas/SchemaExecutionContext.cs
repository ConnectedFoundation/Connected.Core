using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Specifies the type of database constraint for name generation.
/// </summary>
internal enum ConstraintNameType
{
	/// <summary>
	/// Primary key constraint.
	/// </summary>
	PrimaryKey = 1,

	/// <summary>
	/// Foreign key constraint.
	/// </summary>
	ForeignKey = 2,

	/// <summary>
	/// Unique constraint.
	/// </summary>
	Unique = 3,

	/// <summary>
	/// Check constraint.
	/// </summary>
	Check = 4,

	/// <summary>
	/// Default value constraint.
	/// </summary>
	Default = 5,

	/// <summary>
	/// Index.
	/// </summary>
	Index = 6
}

/// <summary>
/// Provides the execution context for Oracle schema synchronization operations.
/// </summary>
/// <remarks>
/// This class encapsulates all necessary information for executing schema operations against
/// an Oracle database including the storage provider, schema metadata, and connection string.
/// It serves as a container for passing context information through the schema synchronization
/// pipeline and maintains registries for tracking constraints and indexes during schema
/// modification operations. The context ensures consistent access to database resources and
/// metadata throughout the synchronization process. Oracle-specific features include handling
/// of sequences for auto-increment columns (pre-12c), managing tablespaces, and tracking
/// constraint names in the format that Oracle expects (uppercase by default, case-sensitive
/// when quoted).
/// </remarks>
internal sealed class SchemaExecutionContext(IStorageProvider storage, ISchema schema, string connectionString)
{
	private ExistingSchema? _existingSchema;

	/// <summary>
	/// Gets the Oracle database version from the connection string.
	/// </summary>
	/// <value>
	/// The database server version.
	/// </value>
	/// <remarks>
	/// The version is used to determine available features such as GENERATED AS IDENTITY
	/// (12c+), OFFSET/FETCH pagination (12c+), and other version-specific capabilities.
	/// Oracle versions include 11g, 12c, 18c, 19c, 21c, and 23ai.
	/// </remarks>
	public Version Version { get; } = OracleStorageConnection.ResolveVersion(connectionString);

	/// <summary>
	/// Gets or sets the existing schema loaded from the database.
	/// </summary>
	/// <value>
	/// The existing schema metadata, or <c>null</c> if not loaded or the table doesn't exist.
	/// </value>
	/// <remarks>
	/// When set, this property populates the constraint registry with existing constraint names
	/// to prevent naming conflicts during schema synchronization operations. Oracle constraint
	/// names are limited to 30 characters (pre-12.2) or 128 characters (12.2+) and must be
	/// unique within the schema.
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
			 * Populate constraint registry from existing schema to prevent naming conflicts
			 */
			foreach (var constraint in _existingSchema.Descriptor.Constraints)
			{
				if (string.IsNullOrWhiteSpace(constraint.Name))
					continue;

				switch (constraint.ConstraintType)
				{
					case ConstraintType.Default:
						AddConstraint(ConstraintNameType.Default, constraint.Name);
						break;
					case ConstraintType.PrimaryKey:
						AddConstraint(ConstraintNameType.PrimaryKey, constraint.Name);
						break;
					case ConstraintType.Unique:
						AddConstraint(ConstraintNameType.Unique, constraint.Name);
						break;
					case ConstraintType.ForeignKey:
						AddConstraint(ConstraintNameType.ForeignKey, constraint.Name);
						break;
					case ConstraintType.Check:
						AddConstraint(ConstraintNameType.Check, constraint.Name);
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
	/// Gets the schema definition being synchronized.
	/// </summary>
	public ISchema Schema { get; } = schema;

	/// <summary>
	/// Gets the connection string for the target Oracle database.
	/// </summary>
	/// <remarks>
	/// Oracle connection strings support multiple formats including Easy Connect (host:port/service),
	/// TNS Names (tnsnames.ora aliases), and connection descriptors (full connection specifications).
	/// The connection string may specify Data Source, User ID, Password, and various Oracle-specific
	/// connection properties.
	/// </remarks>
	public string ConnectionString { get; } = connectionString;

	/// <summary>
	/// Gets the registry of constraint names organized by type.
	/// </summary>
	/// <value>
	/// A dictionary mapping constraint types to lists of constraint names.
	/// </value>
	/// <remarks>
	/// Oracle constraint names must be unique within a schema and are limited to 30 characters
	/// in versions prior to 12.2, or 128 characters in 12.2+. The registry helps generate
	/// unique names and avoid conflicts during schema synchronization.
	/// </remarks>
	public Dictionary<ConstraintNameType, List<string>> Constraints { get; } = [];

	/// <summary>
	/// Executes a DDL command text asynchronously.
	/// </summary>
	/// <param name="commandText">The DDL command to execute.</param>
	/// <returns>A task representing the asynchronous execution operation.</returns>
	/// <remarks>
	/// Executes Oracle DDL statements such as CREATE TABLE, ALTER TABLE, CREATE INDEX, etc.
	/// Oracle automatically commits DDL statements, so they cannot be rolled back. The method
	/// uses the storage provider's writer to execute the command against the Oracle database.
	/// </remarks>
	public async Task Execute(string commandText)
	{
		await Storage.Open<AdHocSchemaEntity>().Execute(new SchemaStorageDto(typeof(OracleDataConnection), ConnectionString, new OracleStorageOperation
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
	/// <remarks>
	/// Queries Oracle system views and tables such as ALL_TABLES, ALL_TAB_COLUMNS, ALL_CONSTRAINTS,
	/// ALL_INDEXES, and ALL_SEQUENCES to retrieve metadata about existing database objects. The
	/// method returns a single row or null if no matching data exists.
	/// </remarks>
	public async Task<AdHocSchemaEntity?> Select(string commandText)
	{
		return await Storage.Open<AdHocSchemaEntity>().Select(new SchemaStorageDto(typeof(OracleDataConnection), ConnectionString, new OracleStorageOperation
		{
			CommandText = commandText
		}));
	}

	/// <summary>
	/// Opens a data reader for the specified storage operation.
	/// </summary>
	/// <param name="operation">The storage operation containing the query to execute.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the data reader.</returns>
	/// <remarks>
	/// Opens an Oracle data reader for streaming query results. This is useful for queries that
	/// return multiple rows such as retrieving all columns for a table or all indexes in a schema.
	/// The caller is responsible for closing the reader when done.
	/// </remarks>
	public async Task<IDataReader> OpenReader(IStorageOperation operation)
	{
		return (await Storage.Open<AdHocSchemaEntity>().OpenReaders(new SchemaStorageDto(typeof(OracleDataConnection), ConnectionString, operation)))[0];
	}

	/// <summary>
	/// Adds a constraint name to the registry.
	/// </summary>
	/// <param name="type">The constraint type.</param>
	/// <param name="name">The constraint name to register.</param>
	/// <remarks>
	/// Registers a constraint name to track which names are in use and prevent conflicts.
	/// Oracle constraint names are case-insensitive when unquoted (converted to uppercase)
	/// and case-sensitive when quoted with double quotes.
	/// </remarks>
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
	/// is incremented until a unique name is found. Oracle has constraint name length limits:
	/// 30 characters for versions prior to 12.2, and 128 characters for 12.2+. The method truncates
	/// names if necessary to fit within these limits while maintaining uniqueness.
	/// </remarks>
	public string GenerateConstraintName(string schema, string tableName, ConstraintNameType type)
	{
		var prefix = ConstraintPrefix(type);
		var index = 0;

		/*
		 * Oracle constraint name length limit: 30 chars (pre-12.2) or 128 chars (12.2+)
		 * We'll use 30 to maintain compatibility with older versions
		 */
		const int maxLength = 30;

		while (true)
		{
			var value = $"{prefix}_{schema.ToUpperInvariant()}_{tableName.ToUpperInvariant()}";

			if (index > 0)
				value = $"{value}_{index}";

			/*
			 * Truncate if necessary to fit Oracle's constraint name length limit
			 */
			if (value.Length > maxLength)
				value = value[..maxLength];

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
	/// <remarks>
	/// Checks all constraint types to see if the given name is already in use. Oracle constraint
	/// names must be unique within a schema regardless of constraint type.
	/// </remarks>
	private bool ConstraintNameExists(string value)
	{
		/*
		 * Search through all constraint types to check for name conflicts
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
	/// <remarks>
	/// Returns standard prefixes for Oracle constraint naming conventions:
	/// PK for primary keys, FK for foreign keys, UQ for unique constraints, CK for check
	/// constraints, DF for default constraints, and IX for indexes.
	/// </remarks>
	private static string ConstraintPrefix(ConstraintNameType type)
	{
		return type switch
		{
			ConstraintNameType.PrimaryKey => "PK",
			ConstraintNameType.ForeignKey => "FK",
			ConstraintNameType.Unique => "UQ",
			ConstraintNameType.Check => "CK",
			ConstraintNameType.Default => "DF",
			ConstraintNameType.Index => "IX",
			_ => "CN"
		};
	}
}
