using Connected.Annotations.Entities;
using Connected.Storage.Schemas;
using System.Data;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Queries column definitions from an existing database table.
/// </summary>
/// <remarks>
/// This query transaction executes a query against the INFORMATION_SCHEMA.COLUMNS system view
/// to retrieve detailed metadata about all columns in a table. It creates ExistingColumn instances
/// populated with information including data types, nullability, lengths, precision, scale, and
/// special properties like version columns. The operation handles data type mapping from SQL Server
/// native types to DbType enumeration values and preserves all column characteristics needed for
/// schema comparison operations. Date/time, numeric, and binary columns receive special processing
/// to capture their specific configuration details.
/// </remarks>
internal class Columns(ExistingSchema existing)
	: SynchronizationQuery<List<ISchemaColumn>>
{
	/// <inheritdoc/>
	protected override async Task<List<ISchemaColumn>> OnExecute()
	{
		var result = new List<ISchemaColumn>();

		/*
		 * Execute the query against INFORMATION_SCHEMA.COLUMNS to retrieve column metadata.
		 */
		var rdr = await Context.OpenReader(new SqlStorageOperation { CommandText = CommandText });

		if (rdr is null)
			return result;

		/*
		 * Process each column row from the result set.
		 */
		while (rdr.Read())
		{
			/*
			 * Create a new existing column with basic properties.
			 */
			var column = new ExistingColumn(existing)
			{
				IsNullable = !string.Equals(rdr.GetValue("IS_NULLABLE", string.Empty), "NO", StringComparison.OrdinalIgnoreCase),
				DataType = SchemaExtensions.ToDbType(rdr.GetValue("DATA_TYPE", string.Empty)),
				MaxLength = rdr.GetValue("CHARACTER_MAXIMUM_LENGTH", 0),
				Name = rdr.GetValue("COLUMN_NAME", string.Empty),
			};

			/*
			 * Capture precision and scale for decimal and numeric types.
			 */
			if (column.DataType == DbType.Decimal || column.DataType == DbType.VarNumeric)
			{
				column.Precision = rdr.GetValue("NUMERIC_PRECISION", 0);
				column.Scale = rdr.GetValue("NUMERIC_SCALE", 0);
			}

			/*
			 * Capture fractional seconds precision for temporal types.
			 */
			if (column.DataType == DbType.DateTime2
				 || column.DataType == DbType.Time
				 || column.DataType == DbType.DateTimeOffset)
				column.DatePrecision = rdr.GetValue("DATETIME_PRECISION", 0);

			/*
			 * Determine the specific date/time kind based on the SQL data type.
			 */
			if (column.DataType == DbType.Date)
				column.DateKind = DateKind.Date;
			else if (column.DataType == DbType.DateTime)
			{
				if (string.Compare(rdr.GetValue("DATA_TYPE", string.Empty), "smalldatetime", true) == 0)
					column.DateKind = DateKind.SmallDateTime;
			}
			else if (column.DataType == DbType.DateTime2)
				column.DateKind = DateKind.DateTime2;
			else if (column.DataType == DbType.Time)
				column.DateKind = DateKind.Time;
			/*
			 * Determine the binary storage kind (binary vs varbinary).
			 */
			else if (column.DataType == DbType.Binary)
			{
				if (string.Compare(rdr.GetValue("DATA_TYPE", string.Empty), "varbinary", true) == 0)
					column.BinaryKind = BinaryKind.VarBinary;
				else if (string.Compare(rdr.GetValue("DATA_TYPE", string.Empty), "binary", true) == 0)
					column.BinaryKind = BinaryKind.Binary;
			}

			/*
			 * Identify timestamp/rowversion columns for optimistic concurrency.
			 */
			column.IsVersion = string.Equals(rdr.GetValue("DATA_TYPE", string.Empty), "timestamp", StringComparison.OrdinalIgnoreCase);

			result.Add(column);
		}

		rdr.Close();

		return result;
	}

	/// <summary>
	/// Gets the SQL command text for querying column metadata.
	/// </summary>
	/// <value>
	/// A SQL query that retrieves all columns for the target table from INFORMATION_SCHEMA.COLUMNS.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{Context.Schema.Schema}' AND TABLE_NAME = '{Context.Schema.Name}'");

			return text.ToString();
		}
	}
}
