using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Storage.Sql;
using Connected.Storage.Sql.Transactions;

namespace Connected.Storage.Tests;

/// <summary>
/// RA-453: the SQL Server builders keep the version of a <see cref="ConsistentEntity{TPrimaryKey}"/>. Its <c>ETag</c> is read-only
/// (<see cref="PersistenceMode.Read"/>); a comparison with <c>HasFlag(PersistenceMode.InMemory)</c> is true for every mode because
/// <c>InMemory</c> is 0, so the version was left out of the UPDATE's WHERE and a concurrent writer was never detected.
/// </summary>
[TestClass]
public sealed class SqlVersionTests
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

	private static Row Entity(State state) => new() { State = state, Id = 5, First = "a", Scratch = "x", ETag = "00000000000000ff" };

	private static async Task<SqlStorageOperation> Update()
	{
		// The builders read nothing from the storage while they build the text.
		var operation = await new UpdateCommandBuilder<Row>(null!).Build(Entity(State.Update), null, CancellationToken.None);

		Assert.IsNotNull(operation);

		return operation;
	}

	/// <summary>
	/// The contract <see cref="ConsistentEntity{TPrimaryKey}.ETag"/> documents: the database keeps the version (read only, never
	/// written), it is the version the update checks, and it is marked to take back a value a provider returns.
	/// </summary>
	[TestMethod]
	public void TheVersionIsKeptByTheDatabaseAndMarkedForTheReturnedValue()
	{
		var etag = typeof(ConsistentEntity<int>).GetProperty(nameof(ConsistentEntity<int>.ETag))!;

		Assert.AreEqual(PersistenceMode.Read, etag.GetCustomAttributes(typeof(PersistenceAttribute), true).Cast<PersistenceAttribute>().Single().Mode);
		Assert.IsTrue(etag.GetCustomAttributes(typeof(ETagAttribute), true).Length == 1);
		Assert.IsTrue(etag.GetCustomAttributes(typeof(ReturnValueAttribute), true).Length == 1);
	}

	[TestMethod]
	public async Task AnUpdateNamesTheVersionInItsWhere()
	{
		var text = (await Update()).CommandText!;

		StringAssert.Contains(text[text.IndexOf("WHERE", StringComparison.Ordinal)..], "[eTag] = @eTag");
	}

	[TestMethod]
	public async Task AnUpdateEnablesConcurrency()
	{
		Assert.AreEqual(DataConcurrencyMode.Enabled, (await Update()).Concurrency);
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
