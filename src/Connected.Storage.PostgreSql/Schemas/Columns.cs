using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Retrieves column definitions and constraints from an existing PostgreSQL table.
/// </summary>
/// <remarks>
/// This query transaction reads the structure of an existing table from PostgreSQL system catalogs
/// including columns, data types, constraints, and indexes. It queries information_schema.columns
/// and pg_catalog views to build a complete picture of the table structure. The retrieved information
/// is used for schema comparison to determine necessary modifications during table synchronization.
/// The query constructs an <see cref="ExistingSchema"/> object containing all column definitions
/// and constraint metadata necessary for schema diff operations.
/// </remarks>
internal sealed class Columns
	: SynchronizationQuery<ExistingSchema>
{
	/// <inheritdoc/>
	protected override async Task<ExistingSchema> OnExecute()
	{
		var result = new ExistingSchema
		{
			Descriptor = new ObjectDescriptor()
		};

		var schema = string.IsNullOrWhiteSpace(Context.Schema.Schema) ? "public" : Context.Schema.Schema;

		/*
		 * Query column information from information_schema.columns
		 */
		var rdr = await Context.OpenReader(new PostgreSqlStorageOperation
		{
			CommandText = $@"
				SELECT 
					c.column_name,
					c.data_type,
					c.character_maximum_length,
					c.is_nullable,
					c.column_default,
					CASE WHEN c.column_default LIKE 'nextval%' THEN true ELSE false END as is_identity
				FROM information_schema.columns c
				WHERE c.table_schema = '{schema}' 
					AND c.table_name = '{Context.Schema.Name}'
				ORDER BY c.ordinal_position"
		});

		/*
		 * Read all column definitions
		 */
		while (rdr.Read())
		{
			var columnName = rdr.GetString(0);
			var dataType = rdr.GetString(1);
			var maxLength = rdr.IsDBNull(2) ? 0 : Convert.ToInt32(rdr.GetValue(2));
			var isNullable = rdr.GetString(3).Equals("YES", StringComparison.OrdinalIgnoreCase);
			var defaultValue = rdr.IsDBNull(4) ? null : rdr.GetString(4);
			var isIdentity = rdr.GetBoolean(5);

			result.Descriptor.Columns.Add(new ExistingColumn
			{
				Name = columnName,
				DataType = MapPostgreSqlTypeToDbType(dataType),
				MaxLength = maxLength,
				IsNullable = isNullable,
				DefaultValue = defaultValue,
				IsIdentity = isIdentity,
				IsPrimaryKey = false // Will be set when reading constraints
			});
		}

		rdr.Close();

		/*
		 * Query constraint information from pg_catalog
		 */
		await LoadConstraints(result, schema);

		return result;
	}

	/// <summary>
	/// Loads constraint information for the table.
	/// </summary>
	/// <param name="schema">The existing schema object to populate with constraints.</param>
	/// <param name="schemaName">The schema name.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task LoadConstraints(ExistingSchema schema, string schemaName)
	{
		/*
		 * Query constraints from pg_catalog
		 */
		var rdr = await Context.OpenReader(new PostgreSqlStorageOperation
		{
			CommandText = $@"
				SELECT 
					con.conname as constraint_name,
					con.contype as constraint_type,
					a.attname as column_name
				FROM pg_catalog.pg_constraint con
				JOIN pg_catalog.pg_class rel ON rel.oid = con.conrelid
				JOIN pg_catalog.pg_namespace nsp ON nsp.oid = rel.relnamespace
				LEFT JOIN pg_catalog.pg_attribute a ON a.attrelid = con.conrelid AND a.attnum = ANY(con.conkey)
				WHERE nsp.nspname = '{schemaName}' 
					AND rel.relname = '{Context.Schema.Name}'
				ORDER BY con.conname, a.attnum"
		});

		/*
		 * Read all constraints
		 */
		while (rdr.Read())
		{
			var constraintName = rdr.GetString(0);
			var constraintType = rdr.GetString(1);
			var columnName = rdr.IsDBNull(2) ? null : rdr.GetString(2);

			var existingConstraint = schema.Descriptor?.Constraints.FirstOrDefault(c => c.Name == constraintName);

			if (existingConstraint is null && schema.Descriptor is not null)
			{
				var type = constraintType switch
				{
					"p" => ConstraintType.PrimaryKey,
					"u" => ConstraintType.Unique,
					"f" => ConstraintType.ForeignKey,
					"c" => ConstraintType.Check,
					_ => ConstraintType.Check
				};

				existingConstraint = new ObjectConstraint
				{
					Name = constraintName,
					ConstraintType = type
				};

				schema.Descriptor.Constraints.Add(existingConstraint);

				/*
				 * Mark primary key columns
				 */
				if (type == ConstraintType.PrimaryKey && columnName is not null)
				{
					var column = schema.Descriptor.Columns.FirstOrDefault(c => c.Name == columnName);

					if (column is ExistingColumn existingCol)
						existingCol.IsPrimaryKey = true;
				}
			}

			/*
			 * Add column to constraint if not already added
			 */
			if (columnName is not null && existingConstraint is not null && schema.Descriptor is not null)
			{
				var column = schema.Descriptor.Columns.FirstOrDefault(c => c.Name == columnName);

				if (column is not null && !existingConstraint.Columns.Contains(column))
					existingConstraint.Columns.Add(column);
			}
		}

		rdr.Close();
	}

	/// <summary>
	/// Maps PostgreSQL data types to standard DbType enumeration.
	/// </summary>
	/// <param name="pgType">The PostgreSQL type name.</param>
	/// <returns>The mapped DbType value.</returns>
	private static DbType MapPostgreSqlTypeToDbType(string pgType)
	{
		return pgType.ToLowerInvariant() switch
		{
			"smallint" or "int2" => DbType.Int16,
			"integer" or "int4" => DbType.Int32,
			"bigint" or "int8" => DbType.Int64,
			"real" or "float4" => DbType.Single,
			"double precision" or "float8" => DbType.Double,
			"numeric" or "decimal" => DbType.Decimal,
			"money" => DbType.Currency,
			"character varying" or "varchar" => DbType.String,
			"character" or "char" => DbType.StringFixedLength,
			"text" => DbType.String,
			"bytea" => DbType.Binary,
			"timestamp without time zone" or "timestamp" => DbType.DateTime,
			"timestamp with time zone" or "timestamptz" => DbType.DateTimeOffset,
			"date" => DbType.Date,
			"time without time zone" or "time" => DbType.Time,
			"boolean" or "bool" => DbType.Boolean,
			"uuid" => DbType.Guid,
			"json" or "jsonb" => DbType.String,
			"xml" => DbType.Xml,
			_ => DbType.String
		};
	}
}
