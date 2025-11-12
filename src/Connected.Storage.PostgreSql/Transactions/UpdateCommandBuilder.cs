using Connected.Annotations.Entities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Connected.Storage.PostgreSql.Transactions;

/// <summary>
/// Builds UPDATE commands for PostgreSQL databases.
/// </summary>
/// <remarks>
/// This class generates PostgreSQL UPDATE statements with proper double-quoted identifier escaping
/// and supports optimistic concurrency control through version/ETag columns. The UPDATE statement
/// includes all non-primary-key columns in the SET clause and uses primary key and version columns
/// in the WHERE clause for row identification and concurrency checking. PostgreSQL uses double quotes
/// for identifier escaping instead of SQL Server's square brackets. The class caches generated commands
/// for performance optimization on repeated operations. When version columns are present, the command
/// is marked with concurrency support enabled to allow proper conflict detection.
/// </remarks>
internal sealed class UpdateCommandBuilder
	: CommandBuilder
{
	static UpdateCommandBuilder()
	{
		Cache = new();
	}

	private static ConcurrentDictionary<string, PostgreSqlStorageOperation> Cache { get; }
	private bool SupportsConcurrency { get; set; }

	/// <inheritdoc/>
	protected override bool TryGetExisting(out PostgreSqlStorageOperation? result)
	{
		return Cache.TryGetValue(ResolveEntityTypeName(), out result);
	}

	/// <inheritdoc/>
	protected override async Task<PostgreSqlStorageOperation> OnBuild(CancellationToken cancel)
	{
		if (Schema is null)
			throw new NullReferenceException(nameof(Schema));

		/*
		 * Generate UPDATE statement with PostgreSQL double-quote escaping
		 */
		WriteLine($"UPDATE \"{Schema.Schema}\".\"{Schema.Name}\" SET");

		await WriteAssignments(cancel);
		await WriteWhere(cancel);

		Trim();
		Write(';');

		var result = new PostgreSqlStorageOperation
		{
			CommandText = CommandText,
			Concurrency = SupportsConcurrency ? DataConcurrencyMode.Enabled : DataConcurrencyMode.Disabled
		};

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		Cache.TryAdd(ResolveEntityTypeName(), result);

		return result;
	}

	/// <summary>
	/// Writes the SET clause assignments for the UPDATE statement.
	/// </summary>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// This method generates column assignments for all properties except primary keys and version columns.
	/// Version columns are moved to the WHERE clause for optimistic concurrency control.
	/// </remarks>
	private async Task WriteAssignments(CancellationToken cancel)
	{
		foreach (var property in Properties)
		{
			if (property.GetCustomAttribute<PrimaryKeyAttribute>() is not null || IsVersion(property))
			{
				if (IsVersion(property))
					SupportsConcurrency = true;

				WhereProperties.Add(property);

				continue;
			}

			var parameter = await CreateParameter(property, cancel);

			WriteLine($"\"{ColumnName(property)}\" = {parameter.Name},");
		}

		Trim();
	}

	/// <summary>
	/// Writes the WHERE clause for row identification and concurrency checking.
	/// </summary>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// The WHERE clause includes primary key columns for row identification and version columns
	/// for optimistic concurrency control. PostgreSQL uses double-quoted identifiers.
	/// </remarks>
	private async Task WriteWhere(CancellationToken cancel)
	{
		WriteLine(string.Empty);

		for (var i = 0; i < WhereProperties.Count; i++)
		{
			var property = WhereProperties[i];
			var parameter = await CreateParameter(property, cancel);

			if (i == 0)
				WriteLine($" WHERE \"{ColumnName(property)}\" = {parameter.Name}");
			else
				WriteLine($" AND \"{ColumnName(property)}\" = {parameter.Name}");
		}
	}
}
