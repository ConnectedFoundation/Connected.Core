using Connected.Configuration.Authentication;
using Connected.Entities;
using Connected.Identities;
using Connected.Identities.Dtos;
using Connected.Services;
using Connected.Storage;
using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Net.Tests;

/// <summary>
/// A service that answers through a handler; a call the handler does not answer returns the default of its type.
/// </summary>
public class Stub
	: DispatchProxy
{
	private Func<MethodInfo, object?[], object?>? Handler { get; set; }

	protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
	{
		ArgumentNullException.ThrowIfNull(targetMethod);

		return Handler?.Invoke(targetMethod, args ?? []) ?? Default(targetMethod.ReturnType);
	}

	/// <summary>
	/// Creates a service of the given interface.
	/// </summary>
	internal static T Create<T>(Func<MethodInfo, object?[], object?>? handler = null)
		where T : class
	{
		var service = DispatchProxy.Create<T, Stub>();

		((Stub)(object)service).Handler = handler;

		return service;
	}

	/// <summary>
	/// Wraps a value in a completed task of the method's return type.
	/// </summary>
	internal static object Result(MethodInfo method, object? value)
	{
		var type = method.ReturnType.GetGenericArguments()[0];

		return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type).Invoke(null, [value])!;
	}

	private static object? Default(Type type)
	{
		if (type == typeof(void))
			return null;

		if (type == typeof(Task))
			return Task.CompletedTask;

		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
		{
			var inner = type.GetGenericArguments()[0];

			return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(inner).Invoke(null, [Default(inner)]);
		}

		return type.IsValueType ? Activator.CreateInstance(type) : null;
	}
}

/// <summary>
/// The two-factor settings of a test.
/// </summary>
internal sealed class TestTwoFactorConfiguration
	: ITwoFactorConfiguration
{
	public string? Key { get; init; }
	public bool Required { get; init; }
	public string? Issuer { get; init; }
	public int ChallengeLifetime { get; init; } = ITwoFactorConfiguration.DefaultChallengeLifetime;
	public int MaxAttempts { get; init; } = ITwoFactorConfiguration.DefaultMaxAttempts;
	public int LockoutDuration { get; init; } = ITwoFactorConfiguration.DefaultLockoutDuration;
}

internal sealed class TestAuthenticationConfiguration(ITwoFactorConfiguration twoFactor)
	: IAuthenticationConfiguration
{
	public IJwTokenConfiguration JwToken => throw new NotSupportedException();
	public ITwoFactorConfiguration TwoFactor => twoFactor;
}

internal sealed record TestUser
	: Entity<long>, IUser
{
	public string? FirstName { get; init; }
	public string? LastName { get; init; }
	public string? Email { get; init; }
	public UserStatus Status { get; init; }
	public string Token { get; init; } = default!;
}

internal sealed class TestValidateUserDto
	: Dto, IValidateUserDto
{
	public string User { get; set; } = default!;
	public string? Password { get; set; }
	public bool Permanent { get; set; }
}

/// <summary>
/// The error a database reports when an insert breaks a unique index.
/// </summary>
internal sealed class DuplicateKeyException()
	: DbException("Cannot insert duplicate key.");

/// <summary>
/// Storage held in memory: inserts honour a unique key, updates the ETag, as the database does.
/// </summary>
internal sealed class InMemoryStorage
	: IStorageProvider
{
	private readonly Dictionary<Type, object> _tables = [];

	/// <summary>
	/// Registers a table.
	/// </summary>
	/// <param name="stamp">Returns the stored copy of an entity with the given id and ETag.</param>
	/// <param name="unique">The value no two rows may share, or <see langword="null"/> for none.</param>
	public InMemoryTable<TEntity> Add<TEntity>(Func<TEntity, long, string, TEntity> stamp, Func<TEntity, object>? unique = null)
		where TEntity : IConsistentEntity<long>
	{
		var table = new InMemoryTable<TEntity>(stamp, unique);

		_tables[typeof(TEntity)] = table;

		return table;
	}

	public InMemoryTable<TEntity> Table<TEntity>()
		where TEntity : IEntity => (InMemoryTable<TEntity>)_tables[typeof(TEntity)];

	public IStorage<TEntity> Open<TEntity>()
		where TEntity : IEntity => Table<TEntity>();

	public IStorage<TEntity> Open<TEntity>(StorageConnectionMode mode)
		where TEntity : IEntity => Table<TEntity>();
}

internal sealed class InMemoryTable<TEntity>
	: IStorage<TEntity>
	where TEntity : IEntity
{
	private readonly Func<TEntity, long, string, TEntity> _stamp;
	private readonly Func<TEntity, object>? _unique;
	private long _identity;

	public InMemoryTable(Func<TEntity, long, string, TEntity> stamp, Func<TEntity, object>? unique)
	{
		_stamp = stamp;
		_unique = unique;
	}

	public List<TEntity> Rows { get; } = [];

	/// <summary>
	/// The number of writes to the table.
	/// </summary>
	public int Writes { get; private set; }

	/// <summary>
	/// Runs before the next write, as another request that wrote first would.
	/// </summary>
	public Action? BeforeNextWrite { get; set; }

	/// <summary>
	/// Adds a row as stored, with an id and an ETag.
	/// </summary>
	public TEntity Seed(TEntity entity)
	{
		var row = _stamp(entity, ++_identity, Guid.NewGuid().ToString());

		Rows.Add(row);

		return row;
	}

	/// <summary>
	/// Replaces a row as another request would, giving it a new ETag.
	/// </summary>
	public TEntity Replace(TEntity entity)
	{
		var id = ((IEntity<long>)entity).Id;
		var row = _stamp(entity, id, Guid.NewGuid().ToString());

		Rows[Rows.FindIndex(f => ((IEntity<long>)f).Id == id)] = row;

		return row;
	}

	private IQueryable<TEntity> Query() => Rows.AsQueryable();

	public Expression Expression => Query().Expression;
	public Type ElementType => typeof(TEntity);
	public IQueryProvider Provider => Query().Provider;
	public IEnumerator<TEntity> GetEnumerator() => Rows.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public IImmutableList<IStorageVariable> Variables => [];
	public void AddVariable(string name, object? value) => throw new NotSupportedException();
	public void RemoveVariable(string name) => throw new NotSupportedException();

	public Task<TEntity?> Update(TEntity? entity)
	{
		if (entity is null)
			return Task.FromResult(entity);

		Writes++;

		var before = BeforeNextWrite;

		BeforeNextWrite = null;
		before?.Invoke();

		var id = ((IEntity<long>)entity).Id;
		var index = Rows.FindIndex(f => ((IEntity<long>)f).Id == id);

		switch (entity.State)
		{
			case State.Add:
				if (_unique is not null && Rows.Any(f => Equals(_unique(f), _unique(entity))))
					throw new DuplicateKeyException();

				return Task.FromResult<TEntity?>(Seed(entity));
			case State.Delete:
				if (index >= 0)
					Rows.RemoveAt(index);

				return Task.FromResult<TEntity?>(entity);
			default:
				if (index < 0 || ((IConsistentEntity<long>)Rows[index]).ETag != ((IConsistentEntity<long>)entity).ETag)
					throw new DBConcurrencyException();

				return Task.FromResult<TEntity?>(Replace(entity));
		}
	}

	/// <summary>
	/// Retries like the storage of the framework: three times, reloading and merging again after a concurrency error.
	/// </summary>
	public async Task<TEntity?> Update(TEntity? entity, Func<TEntity, Task<TEntity>> merging, Func<Task<TEntity?>>? concurrencyRetrying, ICallerContext caller,
		IEnumerable<string>? updatingProperties)
	{
		if (entity is null)
			return entity;

		var current = await merging(entity);
		DBConcurrencyException? last = null;

		for (var i = 0; i < 3; i++)
		{
			try
			{
				return await Update(current);
			}
			catch (DBConcurrencyException ex)
			{
				if (concurrencyRetrying is null)
					throw;

				last = ex;

				current = await concurrencyRetrying() ?? throw new NullReferenceException(nameof(entity));
				current = await merging(current);
			}
		}

		throw last!;
	}

	public Task<TEntity?> Update(TEntity? entity, Func<TEntity, Task<TEntity>> merging, Func<Task<TEntity?>>? concurrencyRetrying, ICallerContext caller) =>
		Update(entity, merging, concurrencyRetrying, caller, null);

	public Task<TEntity?> Update<TDto>(TEntity? entity, TDto dto, Func<Task<TEntity?>>? concurrencyRetrying, ICallerContext caller)
		where TDto : IDto => throw new NotSupportedException();

	public Task<IImmutableList<IDataReader>> OpenReaders(IStorageContextDto dto) => throw new NotSupportedException();
	public Task<int> Execute(IStorageContextDto dto) => throw new NotSupportedException();
	public Task<IEnumerable<TEntity>> Query(IStorageContextDto dto) => throw new NotSupportedException();
	public Task<TEntity?> Select(IStorageContextDto dto) => throw new NotSupportedException();
}
