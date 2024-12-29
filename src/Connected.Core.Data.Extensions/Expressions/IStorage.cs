namespace Connected.Data.Expressions;

internal interface IStorage<TEntity> : IQueryable, IQueryable<TEntity>
{
}
