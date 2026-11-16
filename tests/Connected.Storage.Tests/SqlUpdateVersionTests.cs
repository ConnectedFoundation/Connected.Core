using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Storage.Sql;
using Connected.Storage.Sql.Transactions;
using System.Data;
using System.Reflection;

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

	/// <summary>A version nobody takes a returned value for: it is checked, but nothing reads it back.</summary>
	[Table("test")]
	internal sealed record UnboundRow : Entity<int>
	{
		[Ordinal(0), Length(32)]
		public string? First { get; init; }

		[Ordinal(10000), ETag, Persistence(PersistenceMode.Read)]
		public string? ETag { get; init; }
	}

	/// <summary>Members that carry a persistence attribute, one of each mode, and a version that is only in memory.</summary>
	private sealed class Members
	{
		[Persistence(PersistenceMode.InMemory)] public string? Memory { get; set; }
		[Persistence(PersistenceMode.Read)] public string? Read { get; set; }
		[Persistence(PersistenceMode.Write)] public string? Write { get; set; }
		[Persistence(PersistenceMode.ReadWrite)] public string? ReadWrite { get; set; }
		public string? Plain { get; set; }
		[ETag, Persistence(PersistenceMode.Read)] public string? Version { get; set; }
		[ETag, Persistence(PersistenceMode.InMemory)] public string? MemoryVersion { get; set; }
	}

	private static Row Entity(State state, string? eTag = "00000000000000ff") => new() { State = state, Id = 5, First = "a", Scratch = "x", ETag = eTag };

	private static async Task<SqlStorageOperation> Update(string? eTag = "00000000000000ff")
	{
		// The builders read nothing from the storage while they build the text.
		var operation = await new UpdateCommandBuilder<Row>(null!).Build(Entity(State.Update, eTag), null, CancellationToken.None);

		Assert.IsNotNull(operation);

		return operation;
	}

	private static string Where(SqlStorageOperation operation)
	{
		var text = operation.CommandText!;
		var where = text[text.IndexOf("WHERE", StringComparison.Ordinal)..];

		return where[..where.IndexOf(';')];
	}

	[TestMethod]
	public async Task AnUpdateNamesTheVersionInItsWhere()
	{
		StringAssert.Contains(Where(await Update()), "[eTag] = @eTag");
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
	public async Task AnUpdateOfAnEntityWithoutAVersionIsNotConditionalOnItButReturnsTheOneItWrote(string? eTag)
	{
		var operation = await Update(eTag);

		Assert.IsFalse(Where(operation).Contains("eTag"), Where(operation));
		Assert.AreEqual(DataConcurrencyMode.Disabled, operation.Concurrency);
		StringAssert.Contains(Where(operation), "[id] = @id");
		StringAssert.Contains(operation.CommandText!, "IF @@ROWCOUNT > 0 SET @eTag = (SELECT [eTag] FROM");
		Assert.AreEqual(ParameterDirection.Output, operation.Parameters.Single(f => f.Name == "@eTag").Direction);
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
	public async Task AnUpdateTakesTheVersionInAndReadsTheOneItWroteBackThroughTheSameParameter()
	{
		var operation = await Update();

		Assert.AreEqual(ParameterDirection.InputOutput, operation.Parameters.Single(f => f.Name == "@eTag").Direction);
		StringAssert.Contains(operation.CommandText!, "IF @@ROWCOUNT > 0 SET @eTag = (SELECT [eTag] FROM");
	}

	/// <summary>The provider is told how large the version is; no other parameter is sized for it.</summary>
	[TestMethod]
	public async Task OnlyTheVersionParameterIsSizedAndItIsEightBytes()
	{
		var operation = await Update();

		Assert.AreEqual(8, operation.Parameters.Single(f => f.Name == "@eTag").Size);
		Assert.IsTrue(operation.Parameters.Where(f => f.Name != "@eTag").All(f => f.Size == 0));
	}

	[TestMethod]
	public async Task AnInsertWritesNeitherTheVersionNorWhatIsOnlyInMemory()
	{
		var operation = await new InsertCommandBuilder<Row>(null!).Build(Entity(State.Add), null, CancellationToken.None);
		var text = operation!.CommandText!;
		var insert = text[..text.IndexOf(");", StringComparison.Ordinal)];

		Assert.IsFalse(insert.Contains("eTag"), insert);
		Assert.IsFalse(insert.Contains("scratch"), insert);
		StringAssert.Contains(insert, "[first]");
	}

	[TestMethod]
	public async Task AnInsertReadsTheVersionTheDatabaseGaveTheRowIntoAnOutputParameter()
	{
		var operation = await new InsertCommandBuilder<Row>(null!).Build(Entity(State.Add), null, CancellationToken.None);

		StringAssert.Contains(operation!.CommandText!, "SET @eTag = (SELECT [eTag] FROM");
		Assert.AreEqual(ParameterDirection.Output, operation.Parameters.Single(f => f.Name == "@eTag").Direction);
		Assert.AreEqual(8, operation.Parameters.Single(f => f.Name == "@eTag").Size);
	}

	/// <summary>A version that nothing takes a returned value for is checked in the WHERE, but no statement or parameter reads it back.</summary>
	[TestMethod]
	public async Task AVersionNothingBindsIsCheckedButNotReadBack()
	{
		var update = await new UpdateCommandBuilder<UnboundRow>(null!).Build(new UnboundRow { State = State.Update, Id = 5, First = "a", ETag = "00000000000000ff" }, null, CancellationToken.None);
		var insert = await new InsertCommandBuilder<UnboundRow>(null!).Build(new UnboundRow { State = State.Add, First = "a" }, null, CancellationToken.None);

		StringAssert.Contains(update!.CommandText!, "[eTag] = @eTag");
		Assert.IsFalse(update.CommandText!.Contains("@@ROWCOUNT"), update.CommandText);
		Assert.AreEqual(ParameterDirection.Input, update.Parameters.Single(f => f.Name == "@eTag").Direction);
		Assert.IsFalse(insert!.CommandText!.Contains("SET @eTag"), insert.CommandText);
		Assert.IsFalse(insert.Parameters.Any(f => f.Name == "@eTag"));
	}

	/// <summary>The rule the providers share: what carries a persistence attribute stays out of the commands, except a stored version.</summary>
	[TestMethod]
	[DataRow(nameof(Members.Plain), true)]
	[DataRow(nameof(Members.Version), true)]
	[DataRow(nameof(Members.Memory), false)]
	[DataRow(nameof(Members.Read), false)]
	[DataRow(nameof(Members.Write), false)]
	[DataRow(nameof(Members.ReadWrite), false)]
	[DataRow(nameof(Members.MemoryVersion), false)]
	public void OnlyWhatHasNoPersistenceAttributeAndTheStoredVersionTakePartInTheCommands(string member, bool expected)
	{
		Assert.AreEqual(expected, typeof(Members).GetProperty(member, BindingFlags.Public | BindingFlags.Instance)!.IsCommandMember());
	}
}
