namespace Connected.Storage.PostgreSql;

/// <summary>
/// The PostgreSQL counterpart of a SQL Server rowversion.
/// </summary>
/// <remarks>
/// PostgreSQL has no self-updating version column, so the value is the column's default on insert and is assigned by
/// every generated UPDATE. Unlike rowversion it does not change on raw SQL that bypasses the command builders. It is
/// 8 bytes because <see cref="EntityVersion"/> is.
/// </remarks>
internal static class PostgreSqlVersion
{
	public const string Next = "substring(decode(md5(random()::text || clock_timestamp()::text), 'hex') from 1 for 8)";
}
