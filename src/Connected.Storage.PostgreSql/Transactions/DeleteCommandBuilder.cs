using Connected.Annotations.Entities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Connected.Storage.PostgreSql.Transactions;

/// <summary>
/// Builds DELETE commands for PostgreSQL databases.
/// </summary>
/// <remarks>
/// This class generates PostgreSQL DELETE statements with proper double-quoted identifier escaping
/// and uses primary key columns in the WHERE clause for row identification. The DELETE operation
/// targets a specific row based on its primary key value(s). PostgreSQL uses double quotes for
/// identifier escaping instead of SQL Server's square brackets. The class caches generated commands
/// for performance optimization on repeated operations. Only properties marked with PrimaryKeyAttribute
/// are included in the WHERE clause to ensure precise row deletion.
/// </remarks>
internal sealed class DeleteCommandBuilder
	: CommandBuilder
{
	private static readonly ConcurrentDictionary<string, PostgreSqlStorageOperation> _cache;

	static DeleteCommandBuilder()
	{
		_cache = new();
	}

	private static ConcurrentDictionary<string, PostgreSqlStorageOperation> Cache => _cache;

	/// <inheritdoc/>
	protected override async Task<PostgreSqlStorageOperation> OnBuild(CancellationToken cancel)
	{
		if (Schema is null)
			throw new NullReferenceException(nameof(Schema));

		/*
		 * Generate DELETE statement with PostgreSQL double-quote escaping
		 */
		WriteLine($"DELETE FROM \"{Schema.Schema}\".\"{Schema.Name}\" ");
		await WriteWhere(cancel);

		var result = new PostgreSqlStorageOperation { CommandText = CommandText };

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		Cache.TryAdd(ResolveEntityTypeName(), result);

		return result;
	}

	/// <summary>
	/// Writes the WHERE clause using primary key column(s).
	/// </summary>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// The WHERE clause identifies the row to delete using primary key column(s).
	/// PostgreSQL uses double-quoted identifiers.
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

				Write($"\"{columnName}\" = @{columnName}");
			}
		}

		Write(";");
	}

	/// <inheritdoc/>
	protected override bool TryGetExisting(out PostgreSqlStorageOperation? result)
	{
		return Cache.TryGetValue(ResolveEntityTypeName(), out result);
	}
}
