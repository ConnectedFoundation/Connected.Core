using Connected.Annotations.Entities;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;

namespace Connected.Storage.Sql.Transactions;

internal class InsertCommandBuilder
	: CommandBuilder
{
	private static readonly ConcurrentDictionary<string, SqlStorageOperation> _cache;

	static InsertCommandBuilder()
	{
		_cache = [];
	}

	private static ConcurrentDictionary<string, SqlStorageOperation> Cache => _cache;
	private SqlStorageParameter? PrimaryKeyParameter { get; set; }

	protected override async Task<SqlStorageOperation> OnBuild(CancellationToken cancel)
	{
		if (Schema is null)
			throw new NullReferenceException(nameof(Schema));

		WriteLine($"INSERT [{Schema.Schema}].[{Schema.Name}] (");

		await WriteColumns(cancel);
		WriteLine(")");
		Write("VALUES (");
		WriteValues();
		WriteLine(");");
		WriteOutput();

		var result = new SqlStorageOperation { CommandText = CommandText };

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		Cache.TryAdd(ResolveEntityTypeName(), result);

		return result;
	}

	private void WriteOutput()
	{
		if (PrimaryKeyParameter is null)
			return;

		WriteLine($"SET {PrimaryKeyParameter.Name} = scope_identity();");
	}

	private async Task WriteColumns(CancellationToken cancel)
	{
		foreach (var property in Properties)
		{
			if (property.GetCustomAttribute<PrimaryKeyAttribute>() is PrimaryKeyAttribute pk)
			{
				if (pk.IsIdentity)
				{
					PrimaryKeyParameter = await CreateParameter(property, ParameterDirection.Output, cancel);

					continue;
				}
			}
			else if (IsVersion(property))
				continue;

			await CreateParameter(property, cancel);

			Write($"[{ColumnName(property)}], ");
		}

		Trim();
	}

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

	protected override bool TryGetExisting(out SqlStorageOperation? result)
	{
		return Cache.TryGetValue(ResolveEntityTypeName(), out result);
	}
}
