using Connected.Annotations.Entities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Connected.Storage.Sql.Transactions;

internal class DeleteCommandBuilder
	: CommandBuilder
{
	private static readonly ConcurrentDictionary<string, SqlStorageOperation> _cache;

	static DeleteCommandBuilder()
	{
		_cache = new();
	}

	private static ConcurrentDictionary<string, SqlStorageOperation> Cache => _cache;

	protected override async Task<SqlStorageOperation> OnBuild(CancellationToken cancel)
	{
		if (Schema is null)
			throw new NullReferenceException(nameof(Schema));

		WriteLine($"DELETE [{Schema.Schema}].[{Schema.Name}] ");
		await WriteWhere(cancel);

		var result = new SqlStorageOperation { CommandText = CommandText };

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		Cache.TryAdd(ResolveEntityTypeName(), result);

		return result;
	}

	private async Task WriteWhere(CancellationToken cancel)
	{
		Write("WHERE ");

		foreach (var property in Properties)
		{
			if (property.GetCustomAttribute<PrimaryKeyAttribute>() is not null)
			{
				var columnName = ColumnName(property);

				await CreateParameter(property, cancel);

				Write($"[{columnName}] = @{columnName}");
			}
		}

		Write(";");
	}

	protected override bool TryGetExisting(out SqlStorageOperation? result)
	{
		return Cache.TryGetValue(ResolveEntityTypeName(), out result);
	}
}