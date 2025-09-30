using System.Collections.Immutable;

namespace Connected.Services;

public interface IOperationState
{
	TEntity? SetState<TEntity>(TEntity? entity);
	TEntity? GetState<TEntity>();

	TEntity? SetState<TEntity, TPrimaryKey>(TEntity? entity, TPrimaryKey id);
	TEntity? GetState<TEntity, TPrimaryKey>(TPrimaryKey id);
	IImmutableList<TEntity> GetStates<TEntity>();
}
