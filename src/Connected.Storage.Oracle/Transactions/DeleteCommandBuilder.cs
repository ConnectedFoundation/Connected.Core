using Connected.Annotations.Entities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Connected.Storage.Oracle.Transactions;

/// <summary>
/// Builds DELETE commands for Oracle databases.
/// </summary>
/// <remarks>
/// This class generates Oracle DELETE statements with proper double-quoted identifier escaping,
/// colon-prefixed bind variables, and uses primary key columns in the WHERE clause for row
/// identification. The DELETE operation targets a specific row based on its primary key value(s).
/// Oracle uses double quotes for identifier escaping and colon prefix for bind variables (:param).
/// The class caches generated commands for performance optimization on repeated operations. Only
/// properties marked with PrimaryKeyAttribute are included in the WHERE clause to ensure precise
/// row deletion. Oracle automatically commits DELETE statements unless executed within an explicit
/// transaction.
/// </remarks>
internal sealed class DeleteCommandBuilder
	: CommandBuilder
{
	private static readonly ConcurrentDictionary<string, OracleStorageOperation> _cache;

	static DeleteCommandBuilder()
	{
		_cache = new();
	}

	private static ConcurrentDictionary<string, OracleStorageOperation> Cache => _cache;

	/// <inheritdoc/>
	protected override async Task<OracleStorageOperation> OnBuild(CancellationToken cancel)
	{
		if (Schema is null)
			throw new NullReferenceException(nameof(Schema));

		/*
		 * Generate DELETE statement with Oracle double-quote escaping
		 */
		WriteLine($"DELETE FROM \"{Schema.Schema}\".\"{Schema.Name}\" ");
		await WriteWhere(cancel);

		var result = new OracleStorageOperation { CommandText = CommandText };

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		Cache.TryAdd(ResolveEntityTypeName(), result);

		return result;
	}

	/// <summary>
	/// Writes the WHERE clause using primary key column(s) with Oracle bind variable syntax.
	/// </summary>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// The WHERE clause identifies the row to delete using primary key column(s).
	/// Oracle uses double-quoted identifiers and colon-prefixed bind variables.
	/// </remarks>
	private async Task WriteWhere(CancellationToken cancel)
	{
		Write("WHERE ");

		foreach (var property in Properties)
		{
			if (property.GetCustomAttribute<PrimaryKeyAttribute>() is not null)
			{
				var columnName = ColumnName(property);

				await CreateParameter(property, cancel);

				/*
				 * Oracle uses colon-prefixed bind variables
				 */
				Write($"\"{columnName}\" = :{columnName}");
			}
		}

		Write(";");
	}

	/// <inheritdoc/>
	protected override bool TryGetExisting(out OracleStorageOperation? result)
	{
		return Cache.TryGetValue(ResolveEntityTypeName(), out result);
	}
}
