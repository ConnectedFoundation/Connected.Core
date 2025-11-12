using Connected.Storage.Schemas;
using System.Data;
using System.Text;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Copies data from an existing Oracle table to a temporary table during schema recreation.
/// </summary>
/// <remarks>
/// This transaction generates and executes an INSERT INTO SELECT statement to copy data
/// from the original table to a temporary table. It is used during table recreation operations
/// where the table structure must be dropped and recreated. The operation intelligently handles
/// data type conversions when column types have changed, ensuring data is properly converted
/// to match the new schema using Oracle's CAST function. Version/ETag columns are excluded from
/// copying as they are automatically managed. Only columns that exist in both schemas are copied
/// to avoid data loss while accommodating schema changes. Oracle automatically commits the INSERT
/// statement. Identity columns (GENERATED AS IDENTITY) are automatically populated by Oracle and
/// should be excluded from the column list.
/// </remarks>
internal sealed class DataCopy(ExistingSchema existing, string temporaryName)
	: TableTransaction
{
	private ExistingSchema Existing { get; } = existing;

	/// <summary>
	/// Gets the name of the temporary table where data will be copied.
	/// </summary>
	public string TemporaryName { get; } = temporaryName;

	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the INSERT INTO SELECT statement to copy data from the original table
		 * to the temporary table. Oracle automatically commits DDL/DML statements.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for copying data.
	/// </summary>
	/// <value>
	/// The INSERT INTO SELECT statement with appropriate Oracle CAST conversions.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();
			var columnSet = new StringBuilder();
			var sourceSet = new StringBuilder();
			var comma = string.Empty;

			/*
			 * Build the column list and source expressions, handling type conversions
			 * where necessary when column types have changed.
			 */
			foreach (var column in Context.Schema.Columns)
			{
				/*
				 * Skip version/ETag columns as they are auto-generated.
				 * Skip identity columns as they are auto-populated by Oracle.
				 */
				if (column.IsVersion || column.IsIdentity)
					continue;

				/*
				 * Find the corresponding column in the existing schema.
				 */
				var existing = Existing.Columns.FirstOrDefault(f => string.Equals(column.Name, f.Name, StringComparison.OrdinalIgnoreCase));

				if (existing is null)
					continue;

				columnSet.Append($"{comma}{Escape(column.Name)}");

				/*
				 * Apply type conversion using CAST if the data type, precision, or scale has changed
				 * and the column type requires conversion.
				 */
				if (NeedsConversion(column) && (existing.DataType != column.DataType || existing.Precision != column.Precision || existing.Scale != column.Scale))
					sourceSet.Append($"{comma}CAST({Escape(column.Name)} AS {ConversionString(column)})");
				else
					sourceSet.Append($"{comma}{Escape(column.Name)}");

				comma = ",";
			}

			/*
			 * Generate the complete INSERT INTO SELECT statement.
			 * Oracle doesn't have IF EXISTS syntax like SQL Server, but the INSERT will
			 * succeed even if source table is empty.
			 */
			text.AppendLine($"INSERT INTO {Escape(Context.Schema.Schema, TemporaryName)} ({columnSet.ToString()})");
			text.AppendLine($"SELECT {sourceSet.ToString()} FROM {Escape(Existing.Schema, Existing.Name)}");

			return text.ToString();
		}
	}

	/// <summary>
	/// Generates the Oracle type conversion string for a column.
	/// </summary>
	/// <param name="column">The column to generate conversion string for.</param>
	/// <returns>The Oracle type name with precision and scale if applicable.</returns>
	/// <exception cref="NotSupportedException">Thrown when the data type is not supported for conversion.</exception>
	/// <remarks>
	/// Oracle uses NUMBER type for all numeric values with configurable precision and scale.
	/// </remarks>
	private static string ConversionString(ISchemaColumn column)
	{
		return column.DataType switch
		{
			DbType.Byte => "NUMBER(3)",
			DbType.Currency => "NUMBER(19,4)",
			DbType.Decimal => $"NUMBER({column.Precision}, {column.Scale})",
			DbType.Double => "BINARY_DOUBLE",
			DbType.Int16 => "NUMBER(5)",
			DbType.Int32 => "NUMBER(10)",
			DbType.Int64 => "NUMBER(19)",
			DbType.SByte => "NUMBER(3)",
			DbType.Single => "BINARY_FLOAT",
			DbType.UInt16 => "NUMBER(5)",
			DbType.UInt32 => "NUMBER(10)",
			DbType.UInt64 => "NUMBER(19)",
			DbType.VarNumeric => $"NUMBER({column.Precision}, {column.Scale})",
			_ => throw new NotSupportedException(),
		};
	}

	/// <summary>
	/// Determines whether a column type requires explicit conversion.
	/// </summary>
	/// <param name="column">The column to check.</param>
	/// <returns><c>true</c> if the column requires conversion; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// Numeric types generally require explicit conversion to handle precision and scale changes.
	/// Oracle's NUMBER type is flexible but explicit CAST ensures correct precision/scale.
	/// </remarks>
	private static bool NeedsConversion(ISchemaColumn column)
	{
		return column.DataType switch
		{
			DbType.Byte or DbType.Currency or DbType.Decimal or DbType.Double or DbType.Int16 or DbType.Int32 or DbType.Int64 or DbType.SByte
			or DbType.Single or DbType.UInt16 or DbType.UInt32 or DbType.UInt64 or DbType.VarNumeric => true,
			_ => false,
		};
	}
}
