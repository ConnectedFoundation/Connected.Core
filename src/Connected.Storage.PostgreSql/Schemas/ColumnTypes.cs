using System.Data;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// What the schema synchronization knows about a PostgreSQL column type, in one place.
/// </summary>
/// <remarks>
/// The DDL (<see cref="SynchronizationCommand"/>) and the comparison with the database (<see cref="ColumnComparer"/>) must agree
/// on which types carry a length: a length that is written but not read back alters the column on every start, and one that is
/// read but not written is refused by the server.
/// </remarks>
internal static class ColumnTypes
{
	/// <summary>
	/// Determines whether the type is created with a length and reported with one.
	/// </summary>
	/// <param name="type">The data type to check.</param>
	/// <returns><c>true</c> for the character types.</returns>
	/// <remarks>
	/// Binary is not one of them: <c>bytea</c> has no length, PostgreSQL refuses <c>bytea(n)</c> with "type modifier is not
	/// allowed for type bytea", and information_schema reports none.
	/// </remarks>
	public static bool HasLength(DbType type)
	{
		return type is DbType.String or DbType.StringFixedLength or DbType.AnsiString or DbType.AnsiStringFixedLength;
	}
}
