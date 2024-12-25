﻿using Connected.Storage.Transactions;
using System.Collections.Concurrent;

namespace Connected.Services;

public abstract class ServiceOperation<TDto> : IServiceOperation<TDto>, ITransactionClient, IOperationState
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

	public TEntity? SetState<TEntity>(TEntity? entity)
	{
		var key = typeof(TEntity).FullName;

		if (string.IsNullOrEmpty(key))
			return entity;

		State.AddOrUpdate(key, entity, (existing, @new) =>
		{
			return @new;
		});

		return entity;
	}

	public TEntity? GetState<TEntity>()
	{
		var key = typeof(TEntity).FullName;

		if (string.IsNullOrEmpty(key))
			return default;

		if (!State.TryGetValue(key, out object? result))
			return default;

		return (TEntity?)Convert.ChangeType(result, typeof(TEntity));
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
