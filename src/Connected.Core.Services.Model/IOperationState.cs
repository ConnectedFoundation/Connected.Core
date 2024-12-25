namespace Connected.Services;

public interface IOperationState
{
	TEntity? SetState<TEntity>(TEntity? entity);
	TEntity? GetState<TEntity>();
}
