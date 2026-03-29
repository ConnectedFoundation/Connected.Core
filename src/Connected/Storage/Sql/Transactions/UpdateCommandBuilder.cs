using Connected.Annotations.Entities;
using Connected.Entities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Connected.Storage.Sql.Transactions;

internal sealed class UpdateCommandBuilder<TEntity>(IStorage<TEntity> storage)
	: CommandBuilder<TEntity>(storage)
	where TEntity : IEntity
{
	static UpdateCommandBuilder()
	{
		Cache = new();
	}

	private static ConcurrentDictionary<string, SqlStorageOperation> Cache { get; }
	private bool SupportsConcurrency { get; set; }

	protected override bool TryGetExisting(out SqlStorageOperation? result)
	{
		return Cache.TryGetValue(ResolveEntityTypeName(), out result);
	}

	protected override async Task<SqlStorageOperation> OnBuild(CancellationToken cancel)
	{
		if (Schema is null)
			throw new NullReferenceException(nameof(Schema));

		WriteLine($"UPDATE [{Schema.Schema}].[{Schema.Name}] SET");

		await WriteAssignments(cancel);
		await WriteWhere(cancel);

		Trim();
		Write(';');

		var result = new SqlStorageOperation
		{
			CommandText = CommandText,
			Concurrency = SupportsConcurrency ? DataConcurrencyMode.Enabled : DataConcurrencyMode.Disabled
		};

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		if (UpdatedProperties is not null && UpdatedProperties.Any())
			Cache.TryAdd(ResolveEntityTypeName(), result);

		return result;
	}

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

			if (UpdatedProperties is null)
			{
				var parameter = await CreateParameter(property, cancel);

				WriteLine($"[{ColumnName(property)}] = {parameter.Name},");
			}
		}

		if (UpdatedProperties is not null)
		{
			foreach (var item in UpdatedProperties)
			{
				var property = Properties.FirstOrDefault(f => string.Equals(f.Name, item, StringComparison.OrdinalIgnoreCase));

				if (property is null)
					continue;

				if (IsVersion(property))
					continue;

				var parameter = await CreateParameter(property, cancel);

				WriteLine($"[{ColumnName(property)}] = {parameter.Name},");
			}
		}

		Trim();
	}

	private async Task WriteWhere(CancellationToken cancel)
	{
		WriteLine(string.Empty);

		for (var i = 0; i < WhereProperties.Count; i++)
		{
			var property = WhereProperties[i];
			var parameter = await CreateParameter(property, cancel);

			if (i == 0)
				WriteLine($" WHERE [{ColumnName(property)}] = {parameter.Name}");
			else
				WriteLine($" AND [{ColumnName(property)}] = {parameter.Name}");
		}
	}
}
