using System.Linq.Expressions;

namespace Connected.Entities.Query;

/// <summary>
/// Executes LINQ expression trees asynchronously against a query provider.
/// </summary>
/// <remarks>
/// Implementations translate the provided <see cref="Expression"/> into a
/// storage-specific query and return the projected result. The interface is
/// intentionally small to allow different providers (in-memory, database,
/// remote) to be substituted without coupling callers to a specific
/// query mechanism.
/// </remarks>
public interface IAsyncExecutor
{
	/// <summary>
	/// Executes the specified expression asynchronously and returns the result.
	/// </summary>
	/// <typeparam name="TResult">The expected result type of the expression.</typeparam>
	/// <param name="expression">The expression tree representing the query or projection to execute.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// A task that resolves to the projected result. The result may be <c>null</c>
	/// when the execution yields no value for the requested projection.
	/// </returns>
	Task<TResult?> Execute<TResult>(Expression expression, CancellationToken cancellationToken = default);
}