using Connected.Services.Middlewares;
using Connected.Storage.Transactions;

namespace Connected.Services.Middleware;

public abstract class ServiceOperationMiddleware : MiddlewareComponent, IServiceOperationMiddleware, ITransactionClient
{
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