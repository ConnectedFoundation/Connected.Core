using Connected.Annotations.Entities;
using System.Data;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Retrieves column definitions and constraints from an existing Oracle table.
/// </summary>
/// <remarks>
/// This query transaction reads the structure of an existing table from Oracle data dictionary
/// views including ALL_TAB_COLUMNS, ALL_CONSTRAINTS, and ALL_CONS_COLUMNS. It builds a complete
/// picture of the table structure including columns, data types, constraints, and indexes. The
/// retrieved information is used for schema comparison to determine necessary modifications during
/// table synchronization. The query constructs an <see cref="ExistingSchema"/> object containing
/// all column definitions and constraint metadata necessary for schema diff operations. Oracle
/// stores all metadata in uppercase unless identifiers were quoted during creation.
/// </remarks>
internal sealed class Columns
	: SynchronizationQuery<ExistingSchema>
{
	/// <inheritdoc/>
	protected override async Task<ExistingSchema> OnExecute()
	{
		var result = new ExistingSchema
		{
			Schema = Context.Schema.Schema,
			Name = Context.Schema.Name,
			Type = Context.Schema.Type
		};

		/*
		 * Load column definitions from ALL_TAB_COLUMNS
		 */
		await LoadColumns(result);

		/*
		 * Load constraint information from ALL_CONSTRAINTS and ALL_CONS_COLUMNS
		 */
		await LoadConstraints(result);

		return result;
	}

	/// <summary>
	/// Loads column definitions from ALL_TAB_COLUMNS.
	/// </summary>
	/// <param name="schema">The existing schema object to populate.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task LoadColumns(ExistingSchema schema)
	{
		var schemaName = string.IsNullOrWhiteSpace(Context.Schema.Schema)
			? "USER"
			: Context.Schema.Schema.ToUpperInvariant();

		var tableName = Context.Schema.Name.ToUpperInvariant();

		/*
		 * Query column information from ALL_TAB_COLUMNS
		 */
		var sql = string.IsNullOrWhiteSpace(Context.Schema.Schema)
			? $@"SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH, NULLABLE, DATA_DEFAULT, 
					   DATA_PRECISION, DATA_SCALE
				 FROM USER_TAB_COLUMNS
				 WHERE TABLE_NAME = '{tableName}'
				 ORDER BY COLUMN_ID"
			: $@"SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH, NULLABLE, DATA_DEFAULT,
					   DATA_PRECISION, DATA_SCALE
				 FROM ALL_TAB_COLUMNS
				 WHERE OWNER = '{schemaName}' AND TABLE_NAME = '{tableName}'
				 ORDER BY COLUMN_ID";

		var rdr = await Context.OpenReader(new OracleStorageOperation
		{
			CommandText = sql
		});

		/*
		 * Read all column definitions
		 */
		while (rdr.Read())
		{
			var columnName = rdr.GetString(0);
			var dataType = rdr.GetString(1);
			var dataLength = rdr.IsDBNull(2) ? 0 : Convert.ToInt32(rdr.GetValue(2));
			var isNullable = rdr.GetString(3).Equals("Y", StringComparison.OrdinalIgnoreCase);
			var defaultValue = rdr.IsDBNull(4) ? null : rdr.GetString(4)?.Trim();
			var precision = rdr.IsDBNull(5) ? 0 : Convert.ToInt32(rdr.GetValue(5));
			var scale = rdr.IsDBNull(6) ? 0 : Convert.ToInt32(rdr.GetValue(6));

			var column = new ExistingColumn
			{
				Name = columnName,
				DataType = MapOracleTypeToDbType(dataType),
				MaxLength = dataLength,
				IsNullable = isNullable,
				DefaultValue = string.IsNullOrWhiteSpace(defaultValue) ? null : defaultValue,
				Precision = precision,
				Scale = scale,
				DateKind = DetermineDateKind(dataType),
				BinaryKind = DetermineBinaryKind(dataType),
				IsIdentity = false // Will be determined from constraints or identity columns
			};

			schema.Columns.Add(column);
		}

		rdr.Close();
	}

	/// <summary>
	/// Loads constraint information from ALL_CONSTRAINTS and ALL_CONS_COLUMNS.
	/// </summary>
	/// <param name="schema">The existing schema object to populate with constraints.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task LoadConstraints(ExistingSchema schema)
	{
		var schemaName = string.IsNullOrWhiteSpace(Context.Schema.Schema)
			? "USER"
			: Context.Schema.Schema.ToUpperInvariant();

		var tableName = Context.Schema.Name.ToUpperInvariant();

		/*
		 * Query constraints from ALL_CONSTRAINTS and ALL_CONS_COLUMNS
		 */
		var sql = string.IsNullOrWhiteSpace(Context.Schema.Schema)
			? $@"SELECT c.CONSTRAINT_NAME, c.CONSTRAINT_TYPE, cc.COLUMN_NAME
				 FROM USER_CONSTRAINTS c
				 LEFT JOIN USER_CONS_COLUMNS cc ON c.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
				 WHERE c.TABLE_NAME = '{tableName}'
				 ORDER BY c.CONSTRAINT_NAME, cc.POSITION"
			: $@"SELECT c.CONSTRAINT_NAME, c.CONSTRAINT_TYPE, cc.COLUMN_NAME
				 FROM ALL_CONSTRAINTS c
				 LEFT JOIN ALL_CONS_COLUMNS cc ON c.OWNER = cc.OWNER 
					AND c.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
				 WHERE c.OWNER = '{schemaName}' AND c.TABLE_NAME = '{tableName}'
				 ORDER BY c.CONSTRAINT_NAME, cc.POSITION";

		var rdr = await Context.OpenReader(new OracleStorageOperation
		{
			CommandText = sql
		});

		var constraints = new Dictionary<string, ObjectConstraint>();

		/*
		 * Read all constraints
		 */
		while (rdr.Read())
		{
			var constraintName = rdr.GetString(0);
			var constraintTypeCode = rdr.GetString(1);
			var columnName = rdr.IsDBNull(2) ? null : rdr.GetString(2);

			if (!constraints.TryGetValue(constraintName, out var constraint))
			{
				var type = constraintTypeCode switch
				{
					"P" => ConstraintType.PrimaryKey,
					"U" => ConstraintType.Unique,
					"R" => ConstraintType.ForeignKey,
					"C" => ConstraintType.Check,
					_ => ConstraintType.Check
				};

				constraint = new ObjectConstraint
				{
					Name = constraintName,
					ConstraintType = type
				};

				constraints[constraintName] = constraint;

				schema.Descriptor ??= new ObjectDescriptor();

				schema.Descriptor.Constraints.Add(constraint);
			}

			/*
			 * Add column to constraint
			 */
			if (columnName is not null && !constraint.Columns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
			{
				constraint.Columns.Add(columnName);

				/*
				 * Mark primary key columns
				 */
				if (constraint.ConstraintType == ConstraintType.PrimaryKey)
				{
					var column = schema.Columns.FirstOrDefault(c =>
						string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));

					if (column is ExistingColumn existingCol)
						existingCol.IsPrimaryKey = true;
				}
			}
		}

		rdr.Close();
	}

	/// <summary>
	/// Maps Oracle data types to standard DbType enumeration.
	/// </summary>
	/// <param name="oracleType">The Oracle type name.</param>
	/// <returns>The mapped DbType value.</returns>
	private static DbType MapOracleTypeToDbType(string oracleType)
	{
		return oracleType.ToUpperInvariant() switch
		{
			"NUMBER" => DbType.Decimal,
			"FLOAT" => DbType.Double,
			"BINARY_FLOAT" => DbType.Single,
			"BINARY_DOUBLE" => DbType.Double,
			"VARCHAR2" => DbType.String,
			"NVARCHAR2" => DbType.String,
			"CHAR" => DbType.StringFixedLength,
			"NCHAR" => DbType.StringFixedLength,
			"CLOB" => DbType.String,
			"NCLOB" => DbType.String,
			"BLOB" => DbType.Binary,
			"RAW" => DbType.Binary,
			"LONG RAW" => DbType.Binary,
			"DATE" => DbType.DateTime,
			"TIMESTAMP" => DbType.DateTime2,
			"TIMESTAMP WITH TIME ZONE" => DbType.DateTimeOffset,
			"TIMESTAMP WITH LOCAL TIME ZONE" => DbType.DateTime2,
			"INTERVAL YEAR TO MONTH" => DbType.Int32,
			"INTERVAL DAY TO SECOND" => DbType.Time,
			"ROWID" => DbType.String,
			"UROWID" => DbType.String,
			_ => DbType.String
		};
	}

	/// <summary>
	/// Determines the DateKind for Oracle temporal types.
	/// </summary>
	/// <param name="oracleType">The Oracle type name.</param>
	/// <returns>The appropriate DateKind value.</returns>
	private static DateKind DetermineDateKind(string oracleType)
	{
		return oracleType.ToUpperInvariant() switch
		{
			"DATE" => DateKind.Date,
			"TIMESTAMP" => DateKind.DateTime2,
			"TIMESTAMP WITH TIME ZONE" => DateKind.DateTime,
			"TIMESTAMP WITH LOCAL TIME ZONE" => DateKind.DateTime2,
			"INTERVAL DAY TO SECOND" => DateKind.Time,
			_ => default
		};
	}

	/// <summary>
	/// Determines the BinaryKind for Oracle binary types.
	/// </summary>
	/// <param name="oracleType">The Oracle type name.</param>
	/// <returns>The appropriate BinaryKind value.</returns>
	private static BinaryKind DetermineBinaryKind(string oracleType)
	{
		return oracleType.ToUpperInvariant() switch
		{
			"RAW" => BinaryKind.VarBinary,
			"LONG RAW" => BinaryKind.VarBinary,
			"BLOB" => BinaryKind.Binary,
			_ => default
		};
	}
}
