using Npgsql;
using System.Data.Common;

namespace Connected.Storage.PostgreSql;

/// <summary>
/// An <see cref="NpgsqlCommand"/> that accepts a <see cref="SynchronousTransaction"/>.
/// </summary>
/// <remarks>
/// NpgsqlCommand only takes an NpgsqlTransaction, so this passes on the one the wrapper holds.
/// </remarks>
internal sealed class SynchronousCommand(NpgsqlConnection connection) : NpgsqlCommand(null, connection)
{
	protected override DbTransaction? DbTransaction
	{
		get => base.DbTransaction;
		set => base.DbTransaction = value is SynchronousTransaction transaction ? transaction.Transaction : value;
	}
}
