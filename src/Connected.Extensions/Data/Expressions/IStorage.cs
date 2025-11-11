namespace Connected.Data.Expressions;

/// <summary>
/// Represents a queryable storage abstraction for the specified entity type.
/// Implementations should expose LINQ capabilities via <see cref="IQueryable"/>
/// and <see cref="IQueryable{T}"/> to enable composition of queries.
/// </summary>
/// <typeparam name="TEntity">The entity type stored and queried.</typeparam>
internal interface IStorage<TEntity>
	: IQueryable, IQueryable<TEntity>
{
}
