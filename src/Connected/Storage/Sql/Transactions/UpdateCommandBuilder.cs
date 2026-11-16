using Connected.Annotations.Entities;
using Connected.Entities;
using System.Collections.Concurrent;
using System.Data;
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
		return Cache.TryGetValue(CreateCacheKey(), out result);
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
		WriteVersionOutput();

		var result = new SqlStorageOperation
		{
			CommandText = CommandText,
			Concurrency = SupportsConcurrency ? DataConcurrencyMode.Enabled : DataConcurrencyMode.Disabled
		};

		foreach (var parameter in Parameters)
			result.Parameters.Add(parameter);

		if (UpdatedProperties is not null && UpdatedProperties.Any())
			Cache.TryAdd(CreateCacheKey(), result);

		return result;
	}

	private async Task WriteAssignments(CancellationToken cancel)
	{
		foreach (var property in Properties)
		{
			if (property.GetCustomAttribute<PrimaryKeyAttribute>() is not null || IsVersion(property))
			{
				/*
				 * An entity that carries no version (built from a dto, or returned by an insert, which does not return one) has nothing to
				 * compare: it is updated as before, without the check. A comparison with NULL would match no row and refuse every update.
				 */
				if (IsVersion(property) && string.IsNullOrWhiteSpace(property.GetValue(Entity) as string))
					continue;

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
			/*
			 * The version goes in with the value the entity was read with and comes back with the one the update wrote, which the database
			 * produces; it is bound to the entity's version once the command has run.
			 */
			var parameter = await CreateParameter(property, IsVersion(property) ? ParameterDirection.InputOutput : ParameterDirection.Input, cancel);

			if (i == 0)
				WriteLine($" WHERE [{ColumnName(property)}] = {parameter.Name}");
			else
				WriteLine($" AND [{ColumnName(property)}] = {parameter.Name}");
		}
	}

	/// <summary>
	/// Reads the version the update produced into the version parameter, when the update was conditional on one and changed a row.
	/// </summary>
	/// <remarks>
	/// A SET leaves the number of rows the command affects as the update reported it.
	/// </remarks>
	private void WriteVersionOutput()
	{
		var version = WhereProperties.FirstOrDefault(IsVersion);

		if (version is null)
			return;

		var keys = WhereProperties.Where(f => !IsVersion(f)).Select(f => $"[{ColumnName(f)}] = @{ColumnName(f)}");

		WriteLine(string.Empty);
		Write($"IF @@ROWCOUNT > 0 SET @{ColumnName(version)} = (SELECT [{ColumnName(version)}] FROM [{Schema!.Schema}].[{Schema.Name}] WHERE {string.Join(" AND ", keys)});");
	}
}
