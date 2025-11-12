using Connected.Annotations.Entities;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;

namespace Connected.Storage.PostgreSql.Transactions;

/// <summary>
/// Builds INSERT commands for PostgreSQL databases.
/// </summary>
/// <remarks>
/// This class generates PostgreSQL INSERT statements with proper double-quoted identifier escaping
/// and handles identity columns using RETURNING clause to retrieve generated primary key values.
/// Unlike SQL Server which uses SCOPE_IDENTITY(), PostgreSQL uses the RETURNING clause to get
/// auto-generated values from IDENTITY or SERIAL columns. The class caches generated commands
/// for performance optimization on repeated operations. For non-identity primary keys, the value
/// is included in the INSERT statement. Version/ETag columns are excluded from INSERT operations
/// as they are typically managed by database triggers or default values.
/// </remarks>
internal sealed class InsertCommandBuilder
	: CommandBuilder
{
	private static readonly ConcurrentDictionary<string, PostgreSqlStorageOperation> _cache;

	static InsertCommandBuilder()
	{
		_cache = [];
	}

	private static ConcurrentDictionary<string, PostgreSqlStorageOperation> Cache => _cache;
	private PostgreSqlStorageParameter? PrimaryKeyParameter { get; set; }

	/// <inheritdoc/>
	protected override async Task<PostgreSqlStorageOperation> OnBuild(CancellationToken cancel)
	{
		if (Schema is null)
			throw new NullReferenceException(nameof(Schema));

		/*
		 * Generate INSERT statement with PostgreSQL double-quote escaping
		 */
		WriteLine($"INSERT INTO \"{Schema.Schema}\".\"{Schema.Name}\" (");

		await WriteColumns(cancel);
		WriteLine(")");
		Write("VALUES (");
		WriteValues();
		WriteLine(")");
		WriteReturning();

		var result = new PostgreSqlStorageOperation { CommandText = CommandText };

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		Cache.TryAdd(ResolveEntityTypeName(), result);

		return result;
	}

	/// <summary>
	/// Writes the RETURNING clause for identity columns.
	/// </summary>
	/// <remarks>
	/// PostgreSQL uses RETURNING clause instead of SQL Server's SCOPE_IDENTITY()
	/// to retrieve auto-generated primary key values after INSERT.
	/// </remarks>
	private void WriteReturning()
	{
		if (PrimaryKeyParameter is null)
			return;

		/*
		 * PostgreSQL RETURNING clause to get the generated identity value
		 */
		WriteLine($" RETURNING \"{PrimaryKeyParameter.Name?[1..]}\";");
	}

	/// <summary>
	/// Writes the column list for the INSERT statement.
	/// </summary>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task WriteColumns(CancellationToken cancel)
	{
		foreach (var property in Properties)
		{
			if (property.GetCustomAttribute<PrimaryKeyAttribute>() is PrimaryKeyAttribute pk)
			{
				if (pk.IsIdentity)
				{
					/*
					 * Identity columns are excluded from INSERT and retrieved via RETURNING
					 */
					PrimaryKeyParameter = await CreateParameter(property, ParameterDirection.Output, cancel);

					continue;
				}
			}
			else if (IsVersion(property))
				continue;

			await CreateParameter(property, cancel);

			Write($"\"{ColumnName(property)}\", ");
		}

		Trim();
	}

	/// <summary>
	/// Writes the VALUES clause with parameter placeholders.
	/// </summary>
	private void WriteValues()
	{
		foreach (var property in Properties)
		{
			if (property.GetCustomAttribute<PrimaryKeyAttribute>() is PrimaryKeyAttribute pk && pk.IsIdentity)
				continue;
			else if (IsVersion(property))
				continue;

			Write($"@{ColumnName(property)}, ");
		}

		Trim();
	}

	/// <inheritdoc/>
	protected override bool TryGetExisting(out PostgreSqlStorageOperation? result)
	{
		return Cache.TryGetValue(ResolveEntityTypeName(), out result);
	}
}
