using Connected.Entities;
using Connected.Storage.Transactions;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Services;

public abstract class ServiceOperation<TDto> : IServiceOperation<TDto>, ITransactionClient
	where TDto : IDto
{
	private TDto? _dto;

	protected ServiceOperation()
	{
		State = new();
	}

	private ConcurrentDictionary<string, object?> State { get; }
	public ICallerContext Caller { get; set; } = default!;

	public TDto Dto
	{
		get => _dto ?? throw new NullReferenceException("Dto expected");
		protected set
		{
			if (value is null)
				throw new ArgumentException(nameof(Dto));

			_dto = value;
		}
	}

	public TEntity? SetState<TEntity, TPrimaryKey>(TEntity? entity)
		where TEntity : IPrimaryKeyEntity<TPrimaryKey>
		where TPrimaryKey : notnull
	{
		if (entity is null)
			return default;

		return SetState(entity, entity.Id);
	}
	public TEntity? SetState<TEntity>(TEntity? entity)
	{
		/*
		 * Resolve implemented entity first and then save instance
		 * by its interface because the implementation is not visible
		 * outside the library and implementors cannot access the state
		 * object this way
		 */
		if (typeof(TEntity).IsInterface && typeof(TEntity).IsAssignableTo(typeof(IEntity)))
		{
			var fullName = typeof(TEntity).AssemblyQualifiedName;

			if (fullName is null)
				return entity;

			State.AddOrUpdate(fullName, entity, (existing, @new) =>
			{
				return @new;
			});
		}
		else
		{
			var implementedEntity = typeof(TEntity).ResolveImplementedEntity();
			var key = implementedEntity is not null ? implementedEntity.AssemblyQualifiedName : typeof(TEntity).AssemblyQualifiedName;

			if (string.IsNullOrEmpty(key))
				return entity;

			State.AddOrUpdate(key, entity, (existing, @new) =>
			{
				return @new;
			});
		}

		return entity;
	}

	public TEntity? SetState<TEntity, TPrimaryKey>(TEntity? entity, TPrimaryKey id)
	{
		if (typeof(TEntity).IsInterface && typeof(TEntity).IsAssignableTo(typeof(IEntity)))
		{
			var fullName = typeof(TEntity).AssemblyQualifiedName;

			if (fullName is null)
				return entity;

			State.AddOrUpdate($"{fullName}:{id}", entity, (existing, @new) =>
			{
				return @new;
			});
		}
		else
		{
			var implementedEntity = typeof(TEntity).ResolveImplementedEntity();
			var key = implementedEntity is not null ? implementedEntity.AssemblyQualifiedName : typeof(TEntity).AssemblyQualifiedName;

			if (string.IsNullOrEmpty(key))
				return entity;

			State.AddOrUpdate($"{key}:{id}", entity, (existing, @new) =>
			{
				return @new;
			});
		}

		return entity;
	}

	public TEntity? GetState<TEntity>()
	{
		var key = typeof(TEntity).AssemblyQualifiedName;

		if (string.IsNullOrEmpty(key))
			return default;

		if (State.TryGetValue(key, out object? result) && result is not null)
			return (TEntity)result;
		else
		{
			foreach (var stateKey in State.Keys)
			{
				var instance = State[stateKey];

				if (instance is null)
					continue;

				if (stateKey.StartsWith(key) && instance is TEntity entity)
					return entity;

				if (typeof(TEntity).IsInterface && ResolveStateByInterface<TEntity>(stateKey, instance))
					return (TEntity)instance;
			}
		}

		return default;
	}

	public TEntity? GetState<TEntity, TPrimaryKey>(TPrimaryKey id)
	{
		var key = typeof(TEntity).AssemblyQualifiedName;

		if (string.IsNullOrEmpty(key))
			return default;

		if (!State.TryGetValue($"{key}:{id}", out object? result) || result is null)
			return default;
		else if (typeof(TEntity).IsInterface)
		{
			foreach (var stateKey in State.Keys)
			{
				var tokens = stateKey.Split(':');

				if (tokens.Length != 2)
					continue;

				if (Comparer.DefaultInvariant.Compare(id?.ToString(), tokens[1]) != 0)
					continue;

				var instance = State[stateKey];

				if (instance is null)
					continue;

				if (ResolveStateByInterface<TEntity>(stateKey, instance))
					return (TEntity)instance;
			}
		}

		return (TEntity)result;
	}

	public IImmutableList<TEntity> GetStates<TEntity>()
	{
		var key = typeof(TEntity).AssemblyQualifiedName;

		if (string.IsNullOrEmpty(key))
			return ImmutableList<TEntity>.Empty;
		else
		{
			var result = new List<TEntity>();

			foreach (var stateKey in State.Keys)
			{
				var instance = State[stateKey];

				if (instance is null)
					continue;

				if (stateKey.StartsWith(key) && instance is TEntity entity)
					result.Add(entity);
				else if (typeof(TEntity).IsInterface && ResolveStateByInterface<TEntity>(stateKey, instance))
					result.Add((TEntity)instance);
			}

			return result.ToImmutableList();
		}
	}

	private static bool ResolveStateByInterface<TEntity>(string stateKey, object instance)
	{
		var entityType = typeof(TEntity).AssemblyQualifiedName;

		if (entityType is null)
			return false;

		var type = Type.GetType(stateKey.Split(':')[0]);

		if (type is null)
			return false;

		if (instance.GetType().IsAssignableTo(typeof(TEntity)))
			return true;

		return false;
	}

	async Task ITransactionClient.Commit()
	{
		await OnCommitting();
		await OnCommitted();
	}

	async Task ITransactionClient.Rollback()
	{
		await OnRollingBack();
		await OnRolledBack();
	}

	protected virtual async Task OnCommitted()
	{
		await Task.CompletedTask;
	}

	protected virtual async Task OnRolledBack()
	{
		await Task.CompletedTask;
	}

	protected virtual async Task OnCommitting()
	{
		await Task.CompletedTask;
	}

	protected virtual async Task OnRollingBack()
	{
		await Task.CompletedTask;
	}
}
