using Npgsql;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Connected.Storage.PostgreSql;

/// <summary>
/// Wraps an <see cref="NpgsqlConnection"/> so that closing it and ending its transactions finish before they return.
/// </summary>
/// <remarks>
/// The connection provider commits from an event handler it does not wait for, so a commit still running when
/// it returns races the scope's dispose, which fails with "The connection is already in state 'Executing'".
/// SqlConnection and SqlTransaction do not override CloseAsync, BeginTransactionAsync, CommitAsync or RollbackAsync,
/// so on SQL Server these run synchronously. This class leaves those to the <see cref="DbConnection"/> and
/// <see cref="DbTransaction"/> defaults, which call the synchronous methods, and passes everything else to Npgsql.
/// </remarks>
internal sealed class SynchronousConnection(NpgsqlConnection connection) : DbConnection
{
	[AllowNull]
	public override string ConnectionString
	{
		get => connection.ConnectionString;
		set => connection.ConnectionString = value;
	}

	public override string Database => connection.Database;
	public override string DataSource => connection.DataSource;
	public override string ServerVersion => connection.ServerVersion;
	public override ConnectionState State => connection.State;

	public override void ChangeDatabase(string databaseName) => connection.ChangeDatabase(databaseName);
	public override void Open() => connection.Open();
	public override Task OpenAsync(CancellationToken cancellationToken) => connection.OpenAsync(cancellationToken);
	public override void Close() => connection.Close();

	protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
	{
		return new SynchronousTransaction(this, connection.BeginTransaction(isolationLevel));
	}

	protected override DbCommand CreateDbCommand() => new SynchronousCommand(connection);

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			connection.Dispose();

		base.Dispose(disposing);
	}
}
