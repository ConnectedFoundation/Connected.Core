using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Storage.Sql;
using Connected.Storage.Sql.Transactions;

namespace Connected.Storage.Tests;

/// <summary>
/// The SQL Server builders keep the version of a <see cref="ConsistentEntity{TPrimaryKey}"/> in the UPDATE's WHERE. Its <c>ETag</c> is read
/// only (<see cref="PersistenceMode.Read"/>); a comparison with <c>HasFlag(PersistenceMode.InMemory)</c> is true for every mode because
/// <c>InMemory</c> is 0, so the version was left out of the UPDATE and a concurrent writer was never detected.
/// </summary>
[TestClass]
public sealed class SqlUpdateVersionTests
{
	[Table("test")]
	internal sealed record Row : ConsistentEntity<int>
	{
		[Ordinal(0), Length(32)]
		public string? First { get; init; }

		/// <summary>Kept only in memory: never stored.</summary>
		[Ordinal(1), Persistence(PersistenceMode.InMemory)]
		public string? Scratch { get; init; }
	}

	private static Row Entity(State state, string? eTag = "00000000000000ff") => new() { State = state, Id = 5, First = "a", Scratch = "x", ETag = eTag };

	private static async Task<SqlStorageOperation> Update(string? eTag = "00000000000000ff")
	{
		// The builders read nothing from the storage while they build the text.
		var operation = await new UpdateCommandBuilder<Row>(null!).Build(Entity(State.Update, eTag), null, CancellationToken.None);

		Assert.IsNotNull(operation);

		return operation;
	}

	[TestMethod]
	public async Task AnUpdateNamesTheVersionInItsWhere()
	{
		var text = (await Update()).CommandText!;

		StringAssert.Contains(text[text.IndexOf("WHERE", StringComparison.Ordinal)..], "[eTag] = @eTag");
	}

	[TestMethod]
	public async Task AnUpdateWithAVersionEnablesConcurrency()
	{
		Assert.AreEqual(DataConcurrencyMode.Enabled, (await Update()).Concurrency);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("  ")]
	public async Task AnUpdateOfAnEntityWithoutAVersionIsNotConditionalOnIt(string? eTag)
	{
		var operation = await Update(eTag);
		var text = operation.CommandText!;

		Assert.IsFalse(text.Contains("[eTag]"), text);
		Assert.AreEqual(DataConcurrencyMode.Disabled, operation.Concurrency);
		StringAssert.Contains(text[text.IndexOf("WHERE", StringComparison.Ordinal)..], "[id] = @id");
	}

	[TestMethod]
	public async Task AnUpdateDoesNotWriteTheVersionOrWhatIsOnlyInMemory()
	{
		var text = (await Update()).CommandText!;
		var set = text[..text.IndexOf("WHERE", StringComparison.Ordinal)];

		Assert.IsFalse(set.Contains("[eTag]"), set);
		Assert.IsFalse(set.Contains("[scratch]"), set);
		StringAssert.Contains(set, "[first] = @first");
	}

	[TestMethod]
	public async Task AnInsertWritesNeitherTheVersionNorWhatIsOnlyInMemory()
	{
		var operation = await new InsertCommandBuilder<Row>(null!).Build(Entity(State.Add), null, CancellationToken.None);
		var text = operation!.CommandText!;

		Assert.IsFalse(text.Contains("eTag"), text);
		Assert.IsFalse(text.Contains("scratch"), text);
		StringAssert.Contains(text, "[first]");
	}
}
