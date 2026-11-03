namespace Connected.Storage.PostgreSql;

/// <summary>
/// The PostgreSQL counterpart of a SQL Server rowversion.
/// </summary>
/// <remarks>
/// PostgreSQL has no self-updating version column, so the value is the column's default on insert and is assigned by
/// every generated UPDATE. It comes from one sequence for the whole database, as a rowversion comes from one counter, so
/// a newer write always has a higher version: the shared cache keeps an entity only if its version is not lower than the
/// cached one, and concurrency checks compare versions for equality only. Unlike rowversion it does not change on raw
/// SQL that bypasses the command builders, and there are gaps, as in any sequence. It is 8 bytes, big-endian, because
/// <see cref="EntityVersion"/> is.
/// </remarks>
internal static class PostgreSqlVersion
{
	/// <summary>The sequence every version column draws from.</summary>
	public const string Sequence = "\"public\".\"connectedVersion\"";

	/// <summary>Creates the sequence; it has to exist before a table with a version column is created or altered.</summary>
	public const string CreateSequence = $"CREATE SEQUENCE IF NOT EXISTS {Sequence}";

	/// <summary>The expression for the next version.</summary>
	public const string Next = $"int8send(nextval('{Sequence}'))";
}
