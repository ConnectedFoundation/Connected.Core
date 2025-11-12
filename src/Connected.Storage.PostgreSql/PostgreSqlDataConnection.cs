using Connected.Services;

namespace Connected.Storage.PostgreSql;

/// <summary>
/// Represents a PostgreSQL database connection implementation.
/// </summary>
/// <remarks>
/// This class extends <see cref="PostgreSqlStorageConnection"/> to provide PostgreSQL-specific
/// database connection management. It inherits all connection handling, transaction management,
/// and command execution capabilities from the base PostgreSQL storage connection class while
/// maintaining compatibility with the service cancellation framework.
/// </remarks>
internal sealed class PostgreSqlDataConnection(ICancellationContext context)
	: PostgreSqlStorageConnection(context)
{
}
