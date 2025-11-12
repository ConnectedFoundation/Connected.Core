using Connected.Services;

namespace Connected.Storage.Oracle;

/// <summary>
/// Represents an Oracle database connection implementation.
/// </summary>
/// <remarks>
/// This class extends <see cref="OracleStorageConnection"/> to provide Oracle-specific
/// database connection management. It inherits all connection handling, transaction management,
/// and command execution capabilities from the base Oracle storage connection class while
/// maintaining compatibility with the service cancellation framework. The connection supports
/// Oracle-specific features including PL/SQL procedures, REF CURSORS, sequences, and Oracle
/// native types like NUMBER, VARCHAR2, CLOB, BLOB, and TIMESTAMP.
/// </remarks>
internal sealed class OracleDataConnection(ICancellationContext context)
	: OracleStorageConnection(context)
{
}
