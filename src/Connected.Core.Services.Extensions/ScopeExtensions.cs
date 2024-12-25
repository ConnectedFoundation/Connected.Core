using Connected.Storage.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Connected;
public static class ScopeExtensions
{
	public static async Task Commit(this AsyncServiceScope? scope)
	{
		if (scope?.ServiceProvider.GetService<ITransactionContext>() is ITransactionContext transaction)
			await transaction.Commit();
	}

	public static async Task Flush(this AsyncServiceScope? scope)
	{
		if (scope?.ServiceProvider.GetService<ITransactionContext>() is ITransactionContext transaction)
		{
			if (transaction.State == MiddlewareTransactionState.Active)
				await transaction.Commit();
		}
	}

	public static async Task Rollback(this AsyncServiceScope? scope)
	{
		if (scope?.ServiceProvider.GetService<ITransactionContext>() is ITransactionContext transaction)
			await transaction.Rollback();
	}

	public static async Task Commit(this AsyncServiceScope scope)
	{
		if (scope.ServiceProvider.GetService<ITransactionContext>() is ITransactionContext transaction)
			await transaction.Commit();
	}

	public static async Task Rollback(this AsyncServiceScope scope)
	{
		if (scope.ServiceProvider.GetService<ITransactionContext>() is ITransactionContext transaction)
			await transaction.Rollback();
	}

	public static async Task Flush(this AsyncServiceScope scope)
	{
		if (scope.ServiceProvider.GetService<ITransactionContext>() is ITransactionContext transaction)
		{
			if (transaction.State == MiddlewareTransactionState.Active)
				await transaction.Commit();
		}
	}

}
