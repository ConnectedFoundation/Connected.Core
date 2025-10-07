using Connected.Services;
using Connected.Threading;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;

namespace Connected.Storage;

public abstract class StorageConnection : Middleware, IStorageConnection
{
	private readonly AsyncLocker<int> _lock = new();
	private IDbConnection? _connection;

	protected StorageConnection(ICancellationContext context)
	{
		Context = context;

		Commands = new();
	}

	protected ICancellationContext Context { get; }
	private IDbTransaction? Transaction { get; set; }
	public StorageConnectionMode Mode { get; set; }
	public virtual string? ConnectionString { get; private set; }
	private ConcurrentDictionary<IStorageCommand, IDbCommand> Commands { get; }

	public async Task Initialize(IStorageConnectionDto dto)
	{
		if (ConnectionString is null)
			ConnectionString = dto.ConnectionString;

		Mode = dto.Mode;

		await OnInitialize();
	}

	protected abstract Task<IDbConnection> OnCreateConnection();

	private async Task<IDbConnection> GetConnection()
	{
		if (_connection is null)
		{
			await _lock.LockAsync(1, async () =>
			{
				_connection ??= await OnCreateConnection();
			});
		}

		if (_connection is null)
			throw new NullReferenceException("Couldn't create a connection");

		return _connection;
	}

	public async Task Commit()
	{
		if (Transaction is null || Transaction.Connection is null)
		{
			await Close();

			return;
		}

		await _lock.LockAsync(2, async () =>
		{
			if (Transaction is null || Transaction.Connection is null)
				return;

			for (var i = 0; i < 5; i++)
			{
				if (Transaction is null || Transaction.Connection is null)
					break;

				try
				{
					if (Transaction is DbTransaction db)
						await db.CommitAsync(Context is null ? CancellationToken.None : Context.CancellationToken);
					else
						Transaction.Commit();

					Transaction?.Dispose();
					Transaction = null;

					await Close();

					break;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);

					await Task.Delay(TimeSpan.FromMilliseconds((i + 1) * (i + 1) * 2));
				}
			}
		});

		await Task.CompletedTask;
	}

	public async Task Rollback()
	{
		if (Transaction is null || Transaction.Connection is null)
		{
			await Close();

			return;
		}

		await _lock.LockAsync(3, async () =>
		{
			if (Transaction is null || Transaction.Connection is null)
				return;

			try
			{
				if (Transaction is DbTransaction db)
					await db.RollbackAsync(Context is null ? CancellationToken.None : Context.CancellationToken);
				else
					Transaction.Rollback();

				await Close();
			}
			catch { }
		});

		await Task.CompletedTask;
	}

	private async Task Open()
	{
		var connection = await GetConnection();

		if (connection.State == ConnectionState.Open)
			return;

		await _lock.LockAsync(4, async () =>
			 {
				 if (connection.State != ConnectionState.Closed)
					 return;

				 if (connection is DbConnection db)
					 await db.OpenAsync(Context is null ? CancellationToken.None : Context.CancellationToken);
				 else
					 connection.Open();
			 });

		await Task.CompletedTask;
	}

	public async Task Close()
	{
		if (_connection is null)
			return;

		if (_connection is not null && _connection.State == ConnectionState.Open)
		{
			await _lock.LockAsync(5, async () =>
				 {
					 if (_connection is not null && _connection.State == ConnectionState.Open)
					 {
						 if (Transaction is not null && Transaction.Connection is not null)
						 {
							 try
							 {
								 if (Transaction is DbTransaction db)
									 await db.RollbackAsync(Context is null ? CancellationToken.None : Context.CancellationToken);
								 else
									 Transaction.Rollback();
							 }
							 catch { }

						 }

						 if (_connection is DbConnection dbc)
							 await dbc.CloseAsync();
						 else
							 _connection.Close();
					 }
				 });
		}

		await Task.CompletedTask;
	}

	public async Task<int> Execute(IStorageCommand command)
	{
		await EnsureOpen(true);

		var com = await ResolveCommand(command);

		if (com is null)
			throw new NullReferenceException("Couldn't create a command");

		SetupParameters(command, com);

		if (command.Operation.Parameters is not null)
		{
			foreach (var i in command.Operation.Parameters)
			{
				if (i.Name is null)
					continue;

				SetParameterValue(com, i.Name, i.Value);
			}
		}

		var recordsAffected = await OnExecute(command, com);

		if (command.Operation.Parameters is not null)
		{
			foreach (var i in command.Operation.Parameters)
			{
				if (i.Direction.HasFlag(ParameterDirection.Output) && i.Name is not null)
					i.Value = GetParameterValue(com, i.Name);
			}
		}

		return recordsAffected;

	}
	protected virtual void SetParameterValue(IDbCommand command, string parameterName, object? value)
	{

	}

	protected virtual object? GetParameterValue(IDbCommand command, string parameterName)
	{
		return default;
	}

	protected virtual void SetupParameters(IStorageCommand command, IDbCommand cmd)
	{
	}

	protected virtual async Task<int> OnExecute(IStorageCommand command, IDbCommand cmd)
	{
		if (cmd is DbCommand dbCommand)
			return await dbCommand.ExecuteNonQueryAsync(Context is null ? CancellationToken.None : Context.CancellationToken);
		else
			return cmd.ExecuteNonQuery();
	}

	public virtual async Task<IImmutableList<R>> Query<R>(IStorageCommand command)
	{
		await EnsureOpen(false);

		var com = await ResolveCommand(command);

		if (com is null)
			throw new NullReferenceException("Couldn't create a command");

		IDataReader? rdr = null;

		try
		{
			SetupParameters(command, com);

			if (command.Operation.Parameters is not null)
			{
				foreach (var i in command.Operation.Parameters)
				{
					if (i.Name is null)
						continue;

					SetParameterValue(com, i.Name, i.Value);
				}
			}

			rdr = com is DbCommand db ? await db.ExecuteReaderAsync(Context is null ? CancellationToken.None : Context.CancellationToken) : com.ExecuteReader();
			var result = new List<R>();
			var mappings = new FieldMappings<R>(rdr);

			while (rdr.Read())
			{
				var mappingInstance = await mappings.CreateInstance(rdr, Context is null ? CancellationToken.None : Context.CancellationToken);

				if (mappingInstance is not null)
				{
					OnCreateInstance(command, rdr, mappingInstance);
					result.Add(mappingInstance);
				}
			}

			return result.ToImmutableList();
		}
		finally
		{
			if (rdr != null && !rdr.IsClosed)
			{
				if (rdr is DbDataReader db)
					await db.CloseAsync();
				else
					rdr.Close();
			}
		}
	}

	protected virtual void OnCreateInstance(IStorageCommand command, IDataReader reader, object instance)
	{

	}

	public virtual async Task<R?> Select<R>(IStorageCommand command)
	{
		await EnsureOpen(false);

		var com = await ResolveCommand(command);

		if (com is null)
			throw new NullReferenceException("Couldn't create a command");

		IDataReader? rdr = null;

		try
		{
			SetupParameters(command, com);

			if (command.Operation.Parameters is not null)
			{
				foreach (var i in command.Operation.Parameters)
				{
					if (i.Name is null)
						continue;

					SetParameterValue(com, i.Name, i.Value);
				}
			}

			rdr = com.ExecuteReader(CommandBehavior.SingleRow);
			var mappings = new FieldMappings<R>(rdr);

			if (rdr.Read())
			{
				var result = await mappings.CreateInstance(rdr, Context is null ? CancellationToken.None : Context.CancellationToken);

				if (result is not null)
				{
					OnCreateInstance(command, rdr, result);

					return result;
				}
			}

			return default;
		}
		finally
		{
			if (rdr != null && !rdr.IsClosed)
			{
				if (rdr is DbDataReader db)
					await db.CloseAsync();
				else
					rdr.Close();
			}
		}
	}

	public virtual async Task<IDataReader?> OpenReader(IStorageCommand command)
	{
		await EnsureOpen(false);

		var com = await ResolveCommand(command);

		if (com is null)
			throw new NullReferenceException("Couldn't create a command");

		SetupParameters(command, com);

		if (command.Operation.Parameters is not null)
		{
			foreach (var i in command.Operation.Parameters)
			{
				if (i.Name is null)
					continue;

				SetParameterValue(com, i.Name, i.Value);
			}
		}

		return com.ExecuteReader();
	}

	protected virtual async Task<IDbCommand?> ResolveCommand(IStorageCommand command)
	{
		if (Commands.TryGetValue(command, out IDbCommand? existing))
			return existing;

		if (Commands.TryGetValue(command, out IDbCommand? existing2))
			return existing2;

		return await _lock.LockAsync(6, async () =>
		{
			var connection = await GetConnection();

			var r = connection.CreateCommand();

			r.CommandText = await ProcessCommandText(command);
			r.CommandType = command.Operation.CommandType;
			r.CommandTimeout = command.Operation.CommandTimeout;

			if (Transaction is not null)
				r.Transaction = Transaction;

			Commands.TryAdd(command, r);

			return r;
		});
	}

	protected virtual Task<string?> ProcessCommandText(IStorageCommand command)
	{
		return Task.FromResult(command.Operation.CommandText);
	}

	private async Task EnsureOpen(bool createTransaction)
	{
		var connection = await GetConnection();

		if (connection is null || connection.State == ConnectionState.Open)
		{
			if (createTransaction && Transaction is null)
				await CreateTransaction(connection);

			return;
		}

		await _lock.LockAsync(7, async () =>
		{
			await Open();

			if (createTransaction && Transaction is null)
				await CreateTransaction(connection);
		});
	}

	private async Task CreateTransaction(IDbConnection? connection)
	{
		if (connection is null)
			return;

		Transaction = connection is DbConnection dbc
			? await dbc.BeginTransactionAsync(IsolationLevel.ReadCommitted, Context is null ? CancellationToken.None : Context.CancellationToken)
			: connection.BeginTransaction(IsolationLevel.ReadCommitted);
	}

	protected override void OnDisposing(bool disposing)
	{
		if (!disposing)
			return;

		AsyncUtils.RunSync(() => Close());

		foreach (var command in Commands)
			command.Value.Dispose();

		Commands.Clear();

		if (Transaction is not null)
		{
			try
			{
				AsyncUtils.RunSync(Rollback);

				Transaction.Dispose();
			}
			catch { }

			Transaction = null;
		}

		if (_connection is not null)
		{
			_connection.Dispose();
			_connection = null;
		}
	}

	public async ValueTask DisposeAsync()
	{
		await OnDisposeAsyncCore();

		Dispose(false);
		GC.SuppressFinalize(this);
	}

	protected virtual async ValueTask OnDisposeAsyncCore()
	{
		await Close();

		foreach (var command in Commands)
		{
			if (command.Value is DbCommand db)
				await db.DisposeAsync();
			else
				command.Value.Dispose();
		}

		Commands.Clear();

		if (Transaction is not null)
		{
			try
			{
				//No way to check if possible
				await Rollback();

				if (Transaction is DbTransaction dbt)
					await dbt.DisposeAsync();
				else
					Transaction.Dispose();
			}
			catch { }

			Transaction = null;
		}

		if (_connection is not null)
		{
			if (_connection is DbConnection dbc)
				await dbc.DisposeAsync();
			else
				_connection.Dispose();

			_connection = null;
		}
	}
}