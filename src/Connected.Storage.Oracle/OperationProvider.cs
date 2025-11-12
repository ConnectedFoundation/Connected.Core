using Connected.Annotations;
using Connected.Services;
using Connected.Storage.Oracle.Transactions;

namespace Connected.Storage.Oracle;

/// <summary>
/// Provides Oracle storage operations for entity state management.
/// </summary>
/// <remarks>
/// This sealed class implements <see cref="StorageOperationProvider"/> to generate DML commands
/// (INSERT, UPDATE, DELETE) for entity operations against Oracle databases. It uses the
/// <see cref="AggregatedCommandBuilder{TEntity}"/> to route entities to appropriate command
/// builders based on their state. The provider integrates with the cancellation framework to
/// support graceful operation cancellation. Priority 0 ensures operations are processed before
/// other providers in the middleware pipeline. Generated commands use Oracle-specific syntax
/// including double-quoted identifiers, colon-prefixed bind variables, and RETURNING INTO clauses
/// for identity column retrieval. Oracle DDL/DML statements are automatically committed and cannot
/// be rolled back unless executed within an explicit transaction.
/// </remarks>
[Priority(0)]
internal sealed class OperationProvider(ICancellationContext cancel)
		: StorageOperationProvider
{
	/// <summary>
	/// Gets the cancellation context for operation cancellation support.
	/// </summary>
	private ICancellationContext Cancel { get; } = cancel;

	/// <summary>
	/// Generates a storage operation for the specified entity based on its state.
	/// </summary>
	/// <typeparam name="TEntity">The entity type to create the operation for.</typeparam>
	/// <param name="entity">The entity to create the operation for.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the <see cref="IStorageOperation"/> for the entity, or <c>null</c> if no operation is needed.
	/// </returns>
	/// <remarks>
	/// This method delegates to <see cref="AggregatedCommandBuilder{TEntity}"/> which routes
	/// the entity to the appropriate command builder (Insert, Update, or Delete) based on
	/// the entity's State property. The generated operation includes Oracle-specific SQL
	/// commands with proper parameter binding using colon-prefixed bind variables (:param).
	/// For INSERT operations with identity columns, RETURNING INTO clause retrieves the
	/// generated identity value.
	/// </remarks>
	protected override async Task<IStorageOperation?> OnInvoke<TEntity>(TEntity entity)
	{
		return await AggregatedCommandBuilder<TEntity>.Build(entity, Cancel.CancellationToken);
	}
}
