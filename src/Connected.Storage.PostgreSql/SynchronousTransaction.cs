using Npgsql;
using System.Data;
using System.Data.Common;

namespace Connected.Storage.PostgreSql;

/// <summary>
/// Wraps an <see cref="NpgsqlTransaction"/> so that CommitAsync and RollbackAsync finish before they return.
/// </summary>
/// <remarks>
/// See <see cref="SynchronousConnection"/>. The connection reads as null once the transaction has ended, as Npgsql's does.
/// </remarks>
internal sealed class SynchronousTransaction(SynchronousConnection connection, NpgsqlTransaction transaction) : DbTransaction
{
	public NpgsqlTransaction Transaction => transaction;

	protected override DbConnection? DbConnection => transaction.Connection is null ? null : connection;
	public override IsolationLevel IsolationLevel => transaction.IsolationLevel;

	public override void Commit() => transaction.Commit();
	public override void Rollback() => transaction.Rollback();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			transaction.Dispose();

		base.Dispose(disposing);
	}
}
