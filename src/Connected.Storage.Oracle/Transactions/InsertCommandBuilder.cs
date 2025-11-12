using Connected.Annotations.Entities;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;

namespace Connected.Storage.Oracle.Transactions;

/// <summary>
/// Builds INSERT commands for Oracle databases.
/// </summary>
/// <remarks>
/// This class generates Oracle INSERT statements with proper double-quoted identifier escaping,
/// colon-prefixed bind variables, and handles identity columns using RETURNING INTO clause to
/// retrieve generated primary key values. Oracle 12c+ supports GENERATED AS IDENTITY for auto-
/// increment columns; for earlier versions, sequences with triggers are used. The RETURNING INTO
/// clause returns the generated value into an output parameter. The class caches generated commands
/// for performance optimization on repeated operations. For non-identity primary keys, the value
/// is included in the INSERT statement. Version/ETag columns are excluded from INSERT operations
/// as they are typically managed by database triggers or default values.
/// </remarks>
internal sealed class InsertCommandBuilder
	: CommandBuilder
{
	private static readonly ConcurrentDictionary<string, OracleStorageOperation> _cache;

	static InsertCommandBuilder()
	{
		_cache = [];
	}

	private static ConcurrentDictionary<string, OracleStorageOperation> Cache => _cache;
	private OracleStorageParameter? PrimaryKeyParameter { get; set; }

	/// <inheritdoc/>
	protected override async Task<OracleStorageOperation> OnBuild(CancellationToken cancel)
	{
		if (Schema is null)
			throw new NullReferenceException(nameof(Schema));

		/*
		 * Generate INSERT statement with Oracle double-quote escaping
		 */
		WriteLine($"INSERT INTO \"{Schema.Schema}\".\"{Schema.Name}\" (");

		await WriteColumns(cancel);
		WriteLine(")");
		Write("VALUES (");
		WriteValues();
		WriteLine(")");
		WriteReturning();

		var result = new OracleStorageOperation { CommandText = CommandText };

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		Cache.TryAdd(ResolveEntityTypeName(), result);

		return result;
	}

	/// <summary>
	/// Writes the RETURNING INTO clause for identity columns.
	/// </summary>
	/// <remarks>
	/// Oracle uses RETURNING INTO clause to retrieve auto-generated primary key values after INSERT.
	/// For GENERATED AS IDENTITY columns (12c+), the value is automatically generated.
	/// For sequences (pre-12c), sequence.NEXTVAL is used in the INSERT and RETURNING INTO
	/// retrieves the generated value.
	/// </remarks>
	private void WriteReturning()
	{
		if (PrimaryKeyParameter is null)
			return;

		/*
		 * Oracle RETURNING INTO clause to get the generated identity value
		 * Bind variables use colon prefix in Oracle
		 */
		WriteLine($" RETURNING \"{PrimaryKeyParameter.Name}\" INTO :{PrimaryKeyParameter.Name}");
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
					 * Identity columns are excluded from INSERT and retrieved via RETURNING INTO
					 * For Oracle 12c+, GENERATED AS IDENTITY columns are auto-populated
					 * For earlier versions, sequences are used
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
	/// Writes the VALUES clause with parameter placeholders using Oracle bind variable syntax.
	/// </summary>
	/// <remarks>
	/// Oracle uses colon-prefixed bind variables (:param) instead of at-sign prefix (@param).
	/// </remarks>
	private void WriteValues()
	{
		foreach (var property in Properties)
		{
			if (property.GetCustomAttribute<PrimaryKeyAttribute>() is PrimaryKeyAttribute pk && pk.IsIdentity)
				continue;
			else if (IsVersion(property))
				continue;

			/*
			 * Oracle bind variables use colon prefix
			 */
			Write($":{ColumnName(property)}, ");
		}

		Trim();
	}

	/// <inheritdoc/>
	protected override bool TryGetExisting(out OracleStorageOperation? result)
	{
		return Cache.TryGetValue(ResolveEntityTypeName(), out result);
	}
}
